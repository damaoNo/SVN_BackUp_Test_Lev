# The contents of this file are subject to the Common Public Attribution
# License Version 1.0. (the "License"); you may not use this file except in
# compliance with the License. You may obtain a copy of the License at
# http://code.reddit.com/LICENSE. The License is based on the Mozilla Public
# License Version 1.1, but Sections 14 and 15 have been added to cover use of
# software over a computer network and provide for limited attribution for the
# Original Developer. In addition, Exhibit A has been modified to be consistent
# with Exhibit B.
#
# Software distributed under the License is distributed on an "AS IS" basis,
# WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
# the specific language governing rights and limitations under the License.
#
# The Original Code is reddit.
#
# The Original Developer is the Initial Developer.  The Initial Developer of
# the Original Code is reddit Inc.
#
# All portions of the code written by reddit are Copyright (c) 2006-2015 reddit
# Inc. All Rights Reserved.
###############################################################################

import calendar
from collections import namedtuple
import datetime
from decimal import Decimal, ROUND_DOWN, ROUND_UP
import hashlib
import hmac
import itertools
import json
import random
import time
import urllib
import urlparse

from pylons import g, c
from pylons.i18n import ungettext
from pytz import timezone

from r2.lib import (
    authorize,
    emailer,
    hooks,
)
from r2.lib.db.operators import not_
from r2.lib.db import queries
from r2.lib.cache import sgm
from r2.lib.filters import _force_utf8
from r2.lib.geoip import location_by_ips
from r2.lib.memoize import memoize
from r2.lib.strings import strings
from r2.lib.utils import to_date, weighted_lottery
from r2.models import (
    Account,
    Bid,
    DefaultSR,
    FakeAccount,
    FakeSubreddit,
    Frontpage,
    Link,
    MultiReddit,
    NO_TRANSACTION,
    PromoCampaign,
    PROMOTE_STATUS,
    PromotedLink,
    PromotionLog,
    PromotionWeights,
    Subreddit,
    traffic,
)
from r2.models.keyvalue import NamedGlobals

PROMO_HEALTH_KEY = 'promotions_last_updated'

def _mark_promos_updated():
    NamedGlobals.set(PROMO_HEALTH_KEY, time.time())


def health_check():
    """Calculate the number of seconds since promotions were last updated"""
    return time.time() - int(NamedGlobals.get(PROMO_HEALTH_KEY, default=0))


def cost_per_mille(spend, impressions):
    """Return the cost-per-mille given ad spend and impressions."""
    if impressions:
        return 1000. * float(spend) / impressions
    else:
        return 0


def cost_per_click(spend, clicks):
    """Return the cost-per-click given ad spend and clicks."""
    if clicks:
        return float(spend) / clicks
    else:
        return 0


def promo_keep_fn(item):
    return (is_promoted(item) and
            not item.hidden and
            (c.over18 or not item.over_18))


# attrs

def _base_domain():
    if g.domain_prefix:
        return g.domain_prefix + '.' + g.domain
    else:
        return g.domain

def promo_traffic_url(l): # old traffic url
    return "http://%s/traffic/%s/" % (_base_domain(), l._id36)

def promotraffic_url(l): # new traffic url
    return "http://%s/promoted/traffic/headline/%s" % (_base_domain(), l._id36)

def promo_edit_url(l):
    return "http://%s/promoted/edit_promo/%s" % (_base_domain(), l._id36)

def pay_url(l, campaign):
    return "%spromoted/pay/%s/%s" % (g.payment_domain, l._id36, campaign._id36)

def view_live_url(l, srname):
    domain = _base_domain()
    if srname:
        domain += '/r/%s' % srname
    return 'http://%s/?ad=%s' % (domain, l._fullname)


def refund_url(link, campaign):
    return "%spromoted/refund/%s/%s" % (g.payment_domain, link._id36,
                                        campaign._id36)


# booleans

def is_awaiting_fraud_review(link):
    return link.payment_flagged_reason and link.fraud == None

def is_promo(link):
    return (link and not link._deleted and link.promoted is not None
            and hasattr(link, "promote_status"))

def is_accepted(link):
    return (is_promo(link) and
            link.promote_status != PROMOTE_STATUS.rejected and
            link.promote_status >= PROMOTE_STATUS.accepted)

def is_unpaid(link):
    return is_promo(link) and link.promote_status == PROMOTE_STATUS.unpaid

def is_unapproved(link):
    return is_promo(link) and link.promote_status <= PROMOTE_STATUS.unseen

def is_rejected(link):
    return is_promo(link) and link.promote_status == PROMOTE_STATUS.rejected

def is_promoted(link):
    return is_promo(link) and link.promote_status == PROMOTE_STATUS.promoted

def is_finished(link):
    return is_promo(link) and link.promote_status == PROMOTE_STATUS.finished

def is_live_on_sr(link, sr):
    return bool(live_campaigns_by_link(link, sr=sr))

def is_pending(campaign):
    today = promo_datetime_now().date()
    return today < to_date(campaign.start_date)

def update_query(base_url, query_updates):
    scheme, netloc, path, params, query, fragment = urlparse.urlparse(base_url)
    query_dict = urlparse.parse_qs(query)
    query_dict.update(query_updates)
    query = urllib.urlencode(query_dict)
    return urlparse.urlunparse((scheme, netloc, path, params, query, fragment))


def update_served(items):
    for item in items:
        if not item.promoted:
            continue

        campaign = PromoCampaign._by_fullname(item.campaign)

        if not campaign.has_served:
            campaign.has_served = True
            campaign._commit()


def add_trackers(items, sr, adserver_click_urls=None):
    """Add tracking names and hashes to a list of wrapped promoted links."""
    adserver_click_urls = adserver_click_urls or {}
    for item in items:
        if not item.promoted:
            continue

        tracking_name_fields = [item.fullname, item.campaign]
        if not isinstance(sr, FakeSubreddit):
            tracking_name_fields.append(sr.name)

        tracking_name = '-'.join(tracking_name_fields)

        # construct the impression pixel url
        pixel_mac = hmac.new(
            g.tracking_secret, tracking_name, hashlib.sha1).hexdigest()
        pixel_query = {
            "id": tracking_name,
            "hash": pixel_mac,
            "r": random.randint(0, 2147483647), # cachebuster
        }
        item.imp_pixel = update_query(g.adtracker_url, pixel_query)
        
        if item.third_party_tracking:
            item.third_party_tracking_url = item.third_party_tracking
        if item.third_party_tracking_2:
            item.third_party_tracking_url_2 = item.third_party_tracking_2

        # construct the click redirect url
        item_url = adserver_click_urls.get(item.campaign, item.url)
        url = urllib.unquote(_force_utf8(item_url))
        hashable = ''.join((url, tracking_name.encode("utf-8")))
        click_mac = hmac.new(
            g.tracking_secret, hashable, hashlib.sha1).hexdigest()
        click_query = {
            "id": tracking_name,
            "hash": click_mac,
            "url": url,
        }
        click_url = update_query(g.clicktracker_url, click_query)

        # overwrite the href_url with redirect click_url
        item.href_url = click_url

        # also overwrite the permalink url with redirect click_url for selfposts
        if item.is_self:
            item.permalink = click_url


def update_promote_status(link, status):
    queries.set_promote_status(link, status)
    hooks.get_hook('promote.edit_promotion').call(link=link)


def new_promotion(is_self, title, content, author, ip):
    """
    Creates a new promotion with the provided title, etc, and sets it
    status to be 'unpaid'.
    """
    sr = Subreddit._byID(Subreddit.get_promote_srid())
    l = Link._submit(
        is_self=is_self,
        title=title,
        content=content,
        author=author,
        sr=sr,
        ip=ip,
    )

    l.promoted = True
    l.disable_comments = False
    l.sendreplies = True
    PromotionLog.add(l, 'promotion created')

    update_promote_status(l, PROMOTE_STATUS.unpaid)

    # the user has posted a promotion, so enable the promote menu unless
    # they have already opted out
    if author.pref_show_promote is not False:
        author.pref_show_promote = True
        author._commit()

    # notify of new promo
    emailer.new_promo(l)
    return l


def get_transactions(link, campaigns):
    """Return Bids for specified campaigns on the link.

    A PromoCampaign can have several bids associated with it, but the most
    recent one is recorded on the trans_id attribute. This is the one that will
    be returned.

    """

    campaigns = [c for c in campaigns if (c.trans_id != 0
                                          and c.link_id == link._id)]
    if not campaigns:
        return {}

    bids = Bid.lookup(thing_id=link._id)
    bid_dict = {(b.campaign, b.transaction): b for b in bids}
    bids_by_campaign = {c._id: bid_dict[(c._id, c.trans_id)] for c in campaigns}
    return bids_by_campaign

def new_campaign(link, dates, bid, cpm, target, frequency_cap, frequency_cap_duration,
                 priority, location, platform, mobile_os, ios_devices,
                 ios_version_range, android_devices, android_version_range):
    campaign = PromoCampaign.create(link, target, bid, cpm, dates[0], dates[1],
                                    frequency_cap, frequency_cap_duration, priority,
                                    location, platform, mobile_os, ios_devices,
                                    ios_version_range, android_devices,
                                    android_version_range)
    PromotionWeights.add(link, campaign)
    PromotionLog.add(link, 'campaign %s created' % campaign._id)

    if campaign.priority.cpm:
        author = Account._byID(link.author_id, data=True)
        if getattr(author, "complimentary_promos", False):
            free_campaign(link, campaign, c.user)

    hooks.get_hook('promote.new_campaign').call(link=link, campaign=campaign)
    return campaign


def free_campaign(link, campaign, user):
    auth_campaign(link, campaign, user, -1)

def edit_campaign(link, campaign, dates, bid, cpm, target, frequency_cap,
                  frequency_cap_duration, priority, location, platform='desktop',
                  mobile_os=None, ios_devices=None, ios_version_range=None,
                  android_devices=None, android_version_range=None):
    changed = {}
    if bid != campaign.bid:
         # if the bid amount changed, cancel any pending transactions
        void_campaign(link, campaign, reason='changed_bid')
        changed['bid'] = ("$%0.2f" % campaign.bid, "$%0.2f" % bid)
        hooks.get_hook('promote.edit_bid').call(
            link=link,campaign=campaign, previous=campaign.bid, current=bid)
        campaign.bid = bid
    if dates[0] != campaign.start_date or dates[1] != campaign.end_date:
        original = '%s to %s' % (campaign.start_date, campaign.end_date)
        edited = '%s to %s' % (dates[0], dates[1])
        changed['dates'] = (original, edited)
        campaign.start_date = dates[0]
        campaign.end_date = dates[1]
    if cpm != campaign.cpm:
        changed['cpm'] = (campaign.cpm, cpm)
        campaign.cpm = cpm
    if target != campaign.target:
        changed['target'] = (campaign.target, target)
        campaign.target = target
    if frequency_cap != campaign.frequency_cap:
        changed['frequency_cap'] = (campaign.frequency_cap, frequency_cap)
        campaign.frequency_cap = frequency_cap
    if frequency_cap_duration != campaign.frequency_cap_duration:
        changed['frequency_cap_duration'] = (campaign.frequency_cap_duration,
                                             frequency_cap_duration)
        campaign.frequency_cap_duration = frequency_cap_duration
    if priority != campaign.priority:
        changed['priority'] = (campaign.priority.name, priority.name)
        campaign.priority = priority
    if location != campaign.location:
        changed['location'] = (campaign.location, location)
        campaign.location = location
    if platform != campaign.platform:
        changed["platform"] = (campaign.platform, platform)
        campaign.platform = platform
    if mobile_os != campaign.mobile_os:
        changed["mobile_os"] = (campaign.mobile_os, mobile_os)
        campaign.mobile_os = mobile_os
    if ios_devices != campaign.ios_devices:
        changed['ios_devices'] = (campaign.ios_devices, ios_devices)
        campaign.ios_devices = ios_devices
    if android_devices != campaign.android_devices:
        changed['android_devices'] = (campaign.android_devices, android_devices)
        campaign.android_devices = android_devices
    if ios_version_range != campaign.ios_version_range:
        changed['ios_version_range'] = (campaign.ios_version_range,
                                        ios_version_range)
        campaign.ios_version_range = ios_version_range
    if android_version_range != campaign.android_version_range:
        changed['android_version_range'] = (campaign.android_version_range,
                                            android_version_range)
        campaign.android_version_range = android_version_range

    change_strs = map(lambda t: '%s: %s -> %s' % (t[0], t[1][0], t[1][1]),
                      changed.iteritems())
    change_text = ', '.join(change_strs)
    campaign._commit()

    # update the index
    PromotionWeights.reschedule(link, campaign)

    if campaign.priority.cpm:
        # make it a freebie, if applicable
        author = Account._byID(link.author_id, True)
        if getattr(author, "complimentary_promos", False):
            free_campaign(link, campaign, c.user)

    # record the changes
    if change_text:
        PromotionLog.add(link, 'edited %s: %s' % (campaign, change_text))

    hooks.get_hook('promote.edit_campaign').call(link=link, campaign=campaign)


def terminate_campaign(link, campaign):
    if not is_live_promo(link, campaign):
        return

    now = promo_datetime_now()
    original_end = campaign.end_date
    dates = [campaign.start_date, now]

    # NOTE: this will delete PromotionWeights after and including now.date()
    edit_campaign(link, campaign, dates, campaign.bid, campaign.cpm,
                  campaign.target, campaign.priority, campaign.location)

    campaigns = list(PromoCampaign._by_link(link._id))
    is_live = any(is_live_promo(link, camp) for camp in campaigns
                                            if camp._id != campaign._id)
    if not is_live:
        update_promote_status(link, PROMOTE_STATUS.finished)
        all_live_promo_srnames(_update=True)

    msg = 'terminated campaign %s (original end %s)' % (campaign._id,
                                                        original_end.date())
    PromotionLog.add(link, msg)


def delete_campaign(link, campaign):
    PromotionWeights.delete(link, campaign)
    void_campaign(link, campaign, reason='deleted_campaign')
    campaign.delete()
    PromotionLog.add(link, 'deleted campaign %s' % campaign._id)
    hooks.get_hook('promote.delete_campaign').call(link=link, campaign=campaign)


def void_campaign(link, campaign, reason):
    transactions = get_transactions(link, [campaign])
    bid_record = transactions.get(campaign._id)
    if bid_record:
        a = Account._byID(link.author_id)
        authorize.void_transaction(a, bid_record.transaction, campaign._id)
        campaign.trans_id = NO_TRANSACTION
        campaign._commit()
        text = ('voided transaction for %s: (trans_id: %d)'
                % (campaign, bid_record.transaction))
        PromotionLog.add(link, text)

        if bid_record.transaction > 0:
            # notify the user that the transaction was voided if it was not
            # a freebie
            emailer.void_payment(link, campaign, reason)


def auth_campaign(link, campaign, user, pay_id):
    """
    Authorizes (but doesn't charge) a bid with authorize.net.
    Args:
    - link: promoted link
    - campaign: campaign to be authorized
    - user: Account obj of the user doing the auth (usually the currently
        logged in user)
    - pay_id: customer payment profile id to use for this transaction. (One
        user can have more than one payment profile if, for instance, they have
        more than one credit card on file.) Set pay_id to -1 for freebies.

    Returns: (True, "") if successful or (False, error_msg) if not. 
    """
    void_campaign(link, campaign, reason='changed_payment')
    trans_id, reason = authorize.auth_transaction(campaign.bid, user, pay_id,
                                                  link, campaign._id)

    if trans_id and not reason:
        text = ('updated payment and/or bid for campaign %s: '
                'SUCCESS (trans_id: %d, amt: %0.2f)' % (campaign._id, trans_id,
                                                        campaign.bid))
        PromotionLog.add(link, text)
        if trans_id < 0:
            PromotionLog.add(link, 'FREEBIE (campaign: %s)' % campaign._id)

        if trans_id:
            if is_finished(link):
                # When a finished promo gets a new paid campaign it doesn't
                # need to go through approval again and is marked accepted
                new_status = PROMOTE_STATUS.accepted
            else:
                new_status = max(PROMOTE_STATUS.unseen, link.promote_status)
        else:
            new_status = max(PROMOTE_STATUS.unpaid, link.promote_status)
        update_promote_status(link, new_status)

        if user and (user._id == link.author_id) and trans_id > 0:
            emailer.promo_bid(link, campaign.bid, campaign.start_date)

    else:
        text = ("updated payment and/or bid for campaign %s: FAILED ('%s')"
                % (campaign._id, reason))
        PromotionLog.add(link, text)
        trans_id = 0

    campaign.trans_id = trans_id
    campaign._commit()

    return bool(trans_id), reason



# dates are referenced to UTC, while we want promos to change at (roughly)
# midnight eastern-US.
# TODO: make this a config parameter
timezone_offset = -5 # hours
timezone_offset = datetime.timedelta(0, timezone_offset * 3600)
def promo_datetime_now(offset=None):
    now = datetime.datetime.now(g.tz) + timezone_offset
    if offset is not None:
        now += datetime.timedelta(offset)
    return now


# campaigns can launch the following day if they're created before 17:00 PDT
DAILY_CUTOFF = datetime.time(17, tzinfo=timezone("US/Pacific"))

def get_date_limits(link, is_sponsor=False):
    promo_today = promo_datetime_now().date()

    if is_sponsor:
        min_start = promo_today
    elif is_accepted(link):
        # link is already accepted--let user create a campaign starting
        # tomorrow because it doesn't need to be re-reviewed
        min_start = promo_today + datetime.timedelta(days=1)
    else:
        # campaign and link will need to be reviewed before they can launch.
        # review can happen until DAILY_CUTOFF PDT Monday through Friday and
        # Sunday. Any campaign created after DAILY_CUTOFF is treated as if it
        # were created the following day.
        now = datetime.datetime.now(tz=timezone("US/Pacific"))
        now_today = now.date()
        too_late_for_review = now.time() > DAILY_CUTOFF

        if too_late_for_review and now_today.weekday() == calendar.FRIDAY:
            # no review late on Friday--earliest review is Sunday to launch
            # on Monday
            min_start = now_today + datetime.timedelta(days=3)
        elif now_today.weekday() == calendar.SATURDAY:
            # no review any time on Saturday--earliest review is Sunday to
            # launch on Monday
            min_start = now_today + datetime.timedelta(days=2)
        elif too_late_for_review:
            # no review late in the day--earliest review is tomorrow to
            # launch the following day
            min_start = now_today + datetime.timedelta(days=2)
        else:
            # review will happen today so can launch tomorrow
            min_start = now_today + datetime.timedelta(days=1)

    if is_sponsor:
        max_end = promo_today + datetime.timedelta(days=366)
    else:
        max_end = promo_today + datetime.timedelta(days=93)

    if is_sponsor:
        max_start = max_end - datetime.timedelta(days=1)
    else:
        # authorization hold happens now but expires after 30 days. charge
        # happens 1 day before the campaign launches. the latest a campaign
        # can start is 30 days from now (it will get charged in 29 days).
        max_start = promo_today + datetime.timedelta(days=30)

    return min_start, max_start, max_end


def accept_promotion(link):
    update_promote_status(link, PROMOTE_STATUS.accepted)

    if link._spam:
        link._spam = False
        link._commit()

    emailer.accept_promo(link)

    # if the link has campaigns running now charge them and promote the link
    now = promo_datetime_now()
    campaigns = list(PromoCampaign._by_link(link._id))
    is_live = False
    for camp in campaigns:
        if is_accepted_promo(now, link, camp):
            charge_campaign(link, camp)
            if charged_or_not_needed(camp):
                promote_link(link, camp)
                is_live = True

    if is_live:
        all_live_promo_srnames(_update=True)


def flag_payment(link, reason):
    # already determined to be fraud or already flagged for that reason.
    if link.fraud or reason in link.payment_flagged_reason:
        return

    if link.payment_flagged_reason:
        link.payment_flagged_reason += (", %s" % reason)
    else:
        link.payment_flagged_reason = reason

    link._commit()
    PromotionLog.add(link, "payment flagged: %s" % reason)
    queries.set_payment_flagged_link(link)


def review_fraud(link, is_fraud):
    link.fraud = is_fraud
    link._commit()
    PromotionLog.add(link, "marked as fraud" if is_fraud else "resolved as not fraud")
    queries.unset_payment_flagged_link(link)

    if is_fraud:
        reject_promotion(link, "fraud")
        hooks.get_hook("promote.fraud_identified").call(link=link, sponsor=c.user)


def reject_promotion(link, reason=None):
    was_live = is_promoted(link)
    update_promote_status(link, PROMOTE_STATUS.rejected)
    if reason:
        PromotionLog.add(link, "rejected: %s" % reason)

    # Send a rejection email (unless the advertiser requested the reject)
    if not c.user or c.user._id != link.author_id:
        emailer.reject_promo(link, reason=reason)

    if was_live:
        all_live_promo_srnames(_update=True)


def unapprove_promotion(link):
    if is_unpaid(link):
        return
    elif is_finished(link):
        # when a finished promo is edited it is bumped down to unpaid so if it
        # eventually gets a paid campaign it can get upgraded to unseen and
        # reviewed
        update_promote_status(link, PROMOTE_STATUS.unpaid)
    else:
        update_promote_status(link, PROMOTE_STATUS.unseen)


def authed_or_not_needed(campaign):
    authed = campaign.trans_id != NO_TRANSACTION
    needs_auth = campaign.priority.cpm
    return authed or not needs_auth


def charged_or_not_needed(campaign):
    # True if a campaign has a charged transaction or doesn't need one
    charged = authorize.is_charged_transaction(campaign.trans_id, campaign._id)
    needs_charge = campaign.priority.cpm
    return charged or not needs_charge


def is_accepted_promo(date, link, campaign):
    return (campaign.start_date <= date < campaign.end_date and
            is_accepted(link) and
            authed_or_not_needed(campaign))


def is_scheduled_promo(date, link, campaign):
    return (is_accepted_promo(date, link, campaign) and 
            charged_or_not_needed(campaign))


def is_live_promo(link, campaign):
    now = promo_datetime_now()
    return is_promoted(link) and is_scheduled_promo(now, link, campaign)


def _is_geotargeted_promo(link):
    campaigns = live_campaigns_by_link(link)
    geotargeted = filter(lambda camp: camp.location, campaigns)
    city_target = any(camp.location.metro for camp in geotargeted)
    return bool(geotargeted), city_target


def is_geotargeted_promo(link):
    key = 'geotargeted_promo_%s' % link._id
    from_cache = g.cache.get(key)
    if not from_cache:
        ret = _is_geotargeted_promo(link)
        g.cache.set(key, ret, time=60)
        return ret
    else:
        return from_cache


def get_promos(date, sr_names=None, link=None):
    campaign_ids = PromotionWeights.get_campaign_ids(
        date, sr_names=sr_names, link=link)
    campaigns = PromoCampaign._byID(campaign_ids, data=True, return_dict=False)
    link_ids = {camp.link_id for camp in campaigns}
    links = Link._byID(link_ids, data=True)
    for camp in campaigns:
        yield camp, links[camp.link_id]


def get_accepted_promos(offset=0):
    date = promo_datetime_now(offset=offset)
    for camp, link in get_promos(date):
        if is_accepted_promo(date, link, camp):
            yield camp, link


def get_scheduled_promos(offset=0):
    date = promo_datetime_now(offset=offset)
    for camp, link in get_promos(date):
        if is_scheduled_promo(date, link, camp):
            yield camp, link


def charge_campaign(link, campaign):
    if charged_or_not_needed(campaign):
        return

    user = Account._byID(link.author_id)
    success, reason = authorize.charge_transaction(user, campaign.trans_id,
                                                   campaign._id)

    if not success:
        if reason == authorize.TRANSACTION_NOT_FOUND:
            # authorization hold has expired
            original_trans_id = campaign.trans_id
            campaign.trans_id = NO_TRANSACTION
            campaign._commit()
            text = ('voided expired transaction for %s: (trans_id: %d)'
                    % (campaign, original_trans_id))
            PromotionLog.add(link, text)
        return

    hooks.get_hook('promote.edit_campaign').call(link=link, campaign=campaign)

    if not is_promoted(link):
        update_promote_status(link, PROMOTE_STATUS.pending)

    emailer.queue_promo(link, campaign.bid, campaign.trans_id)
    text = ('auth charge for campaign %s, trans_id: %d' %
            (campaign._id, campaign.trans_id))
    PromotionLog.add(link, text)


def charge_pending(offset=1):
    for camp, link in get_accepted_promos(offset=offset):
        charge_campaign(link, camp)


def live_campaigns_by_link(link, sr=None):
    if not is_promoted(link):
        return []

    sr_names = [sr.name] if sr else None
    now = promo_datetime_now()
    return [camp for camp, link in get_promos(now, sr_names=sr_names,
                                              link=link)
            if is_live_promo(link, camp)]


def promote_link(link, campaign):
    if (not link.over_18 and
        not link.over_18_override and
        any(sr.over_18 for sr in campaign.target.subreddits_slow)):
        link.over_18 = True
        link._commit()

    if not is_promoted(link):
        update_promote_status(link, PROMOTE_STATUS.promoted)
        emailer.live_promo(link)


def make_daily_promotions():
    # charge campaigns so they can go live
    charge_pending(offset=0)
    charge_pending(offset=1)

    # promote links and record ids of promoted links
    link_ids = set()
    for campaign, link in get_scheduled_promos(offset=0):
        link_ids.add(link._id)
        promote_link(link, campaign)

    # expire finished links
    q = Link._query(Link.c.promote_status == PROMOTE_STATUS.promoted, data=True)
    q = q._filter(not_(Link.c._id.in_(link_ids)))
    for link in q:
        update_promote_status(link, PROMOTE_STATUS.finished)
        emailer.finished_promo(link)

    # update subreddits with promos
    all_live_promo_srnames(_update=True)

    _mark_promos_updated()
    finalize_completed_campaigns(daysago=1)
    hooks.get_hook('promote.make_daily_promotions').call(offset=0)


def finalize_completed_campaigns(daysago=1):
    # PromoCampaign.end_date is utc datetime with year, month, day only
    now = datetime.datetime.now(g.tz)
    date = now - datetime.timedelta(days=daysago)
    date = date.replace(hour=0, minute=0, second=0, microsecond=0)

    q = PromoCampaign._query(PromoCampaign.c.end_date == date,
                             # exclude no transaction
                             PromoCampaign.c.trans_id != NO_TRANSACTION,
                             data=True)
    # filter out freebies
    campaigns = filter(lambda camp: camp.trans_id > NO_TRANSACTION, q)

    if not campaigns:
        return

    # check that traffic is up to date
    earliest_campaign = min(campaigns, key=lambda camp: camp.start_date)
    start, end = get_total_run(earliest_campaign)
    missing_traffic = traffic.get_missing_traffic(start.replace(tzinfo=None),
                                                  date.replace(tzinfo=None))
    if missing_traffic:
        raise ValueError("Can't finalize campaigns finished on %s."
                         "Missing traffic from %s" % (date, missing_traffic))

    links = Link._byID([camp.link_id for camp in campaigns], data=True)
    underdelivered_campaigns = []

    for camp in campaigns:
        if hasattr(camp, 'refund_amount'):
            continue

        link = links[camp.link_id]
        billable_impressions = get_billable_impressions(camp)
        billable_amount = get_billable_amount(camp, billable_impressions)

        if billable_amount >= camp.bid:
            if hasattr(camp, 'cpm'):
                text = '%s completed with $%s billable (%s impressions @ $%s).'
                text %= (camp, billable_amount, billable_impressions, camp.cpm)
            else:
                text = '%s completed with $%s billable (pre-CPM).'
                text %= (camp, billable_amount) 
            PromotionLog.add(link, text)
            camp.refund_amount = 0.
            camp._commit()
        elif charged_or_not_needed(camp):
            underdelivered_campaigns.append(camp)

        if underdelivered_campaigns:
            queries.set_underdelivered_campaigns(underdelivered_campaigns)


def get_refund_amount(camp, billable):
    existing_refund = getattr(camp, 'refund_amount', 0.)
    charge = camp.bid - existing_refund
    refund_amount = charge - billable
    refund_amount = Decimal(str(refund_amount)).quantize(Decimal('.01'),
                                                    rounding=ROUND_UP)
    return max(float(refund_amount), 0.)


def refund_campaign(link, camp, billable_amount, billable_impressions):
    refund_amount = get_refund_amount(camp, billable_amount)
    if refund_amount <= 0:
        return

    owner = Account._byID(camp.owner_id, data=True)
    try:
        success = authorize.refund_transaction(owner, camp.trans_id,
                                               camp._id, refund_amount)
    except authorize.AuthorizeNetException as e:
        text = ('%s $%s refund failed' % (camp, refund_amount))
        PromotionLog.add(link, text)
        g.log.debug(text + ' (response: %s)' % e)
        return

    text = ('%s completed with $%s billable (%s impressions @ $%s).'
            ' %s refunded.' % (camp, billable_amount,
                               billable_impressions, camp.cpm,
                               refund_amount))
    PromotionLog.add(link, text)
    camp.refund_amount = refund_amount
    camp._commit()
    queries.unset_underdelivered_campaigns(camp)
    emailer.refunded_promo(link)


PromoTuple = namedtuple('PromoTuple', ['link', 'weight', 'campaign'])


@memoize('all_live_promo_srnames')
def all_live_promo_srnames():
    now = promo_datetime_now()
    srnames = itertools.chain.from_iterable(
        camp.target.subreddit_names for camp, link in get_promos(now)
                                    if is_live_promo(link, camp)
    )
    return set(srnames)


def srnames_from_site(user, site):
    if not isinstance(site, FakeSubreddit):
        srnames = {site.name}
    elif isinstance(site, MultiReddit):
        srnames = {sr.name for sr in site.srs}
    elif user and not isinstance(user, FakeAccount):
        srnames = {sr.name for sr in Subreddit.user_subreddits(user, ids=False)}
        srnames.add(Frontpage.name)
    else:
        srnames = {sr.name for sr in Subreddit.user_subreddits(None, ids=False)}
        srnames.add(Frontpage.name)
    return srnames


def srnames_with_live_promos(user, site):
    site_srnames = srnames_from_site(user, site)
    promo_srnames = all_live_promo_srnames()
    return promo_srnames.intersection(site_srnames)


# special handling for memcache ascii protocol
SPECIAL_NAMES = {" reddit.com": "_reddit.com"}
REVERSED_NAMES = {v: k for k, v in SPECIAL_NAMES.iteritems()}


def _get_live_promotions(sanitized_names):
    now = promo_datetime_now()
    sr_names = [REVERSED_NAMES.get(name, name) for name in sanitized_names]
    ret = {sr_name: [] for sr_name in sanitized_names}
    for camp, link in get_promos(now, sr_names=sr_names):
        if is_live_promo(link, camp):
            weight = (camp.bid / camp.ndays)
            pt = PromoTuple(link=link._fullname, weight=weight,
                            campaign=camp._fullname)
            for sr_name in camp.target.subreddit_names:
                if sr_name in sr_names:
                    sanitized_name = SPECIAL_NAMES.get(sr_name, sr_name)
                    ret[sanitized_name].append(pt)
    return ret


def get_live_promotions(sr_names):
    sanitized_names = [SPECIAL_NAMES.get(name, name) for name in sr_names]
    promos_by_sanitized_name = sgm(
        g.cache, sanitized_names, miss_fn=_get_live_promotions,
        prefix='live_promotions', time=60, stale=True)
    promos_by_srname = {
        REVERSED_NAMES.get(name, name): val
        for name, val in promos_by_sanitized_name.iteritems()
    }
    return itertools.chain.from_iterable(promos_by_srname.itervalues())


def lottery_promoted_links(sr_names, n=10):
    """Run weighted_lottery to order and choose a subset of promoted links."""
    promo_tuples = get_live_promotions(sr_names)

    # house priority campaigns have weight of 0, use some small value
    # so they'll show if there are no other campaigns
    weights = {p: p.weight or 0.001 for p in promo_tuples}
    selected = []
    while weights and len(selected) < n:
        s = weighted_lottery(weights)
        del weights[s]
        selected.append(s)
    return selected


def get_total_run(thing):
    """Return the total time span this link or campaign will run.

    Starts at the start date of the earliest campaign and goes to the end date
    of the latest campaign.

    """

    if isinstance(thing, Link):
        campaigns = PromoCampaign._by_link(thing._id)
    elif isinstance(thing, PromoCampaign):
        campaigns = [thing]

    earliest = None
    latest = None
    for campaign in campaigns:
        if not charged_or_not_needed(campaign):
            continue

        if not earliest or campaign.start_date < earliest:
            earliest = campaign.start_date

        if not latest or campaign.end_date > latest:
            latest = campaign.end_date

    # a manually launched promo (e.g., sr discovery) might not have campaigns.
    if not earliest or not latest:
        latest = datetime.datetime.utcnow()
        earliest = latest - datetime.timedelta(days=30)  # last month

    # ugh this stuff is a mess. they're stored as "UTC" but actually mean UTC-5.
    earliest = earliest.replace(tzinfo=g.tz) - timezone_offset
    latest = latest.replace(tzinfo=g.tz) - timezone_offset

    return earliest, latest


def get_traffic_dates(thing):
    """Retrieve the start and end of a Promoted Link or PromoCampaign."""
    now = datetime.datetime.now(g.tz).replace(minute=0, second=0, microsecond=0)
    start, end = get_total_run(thing)
    end = min(now, end)
    return start, end


def get_billable_impressions(campaign):
    start, end = get_traffic_dates(campaign)
    if start > datetime.datetime.now(g.tz):
        return 0

    traffic_lookup = traffic.TargetedImpressionsByCodename.promotion_history
    imps = traffic_lookup(campaign._fullname, start.replace(tzinfo=None),
                          end.replace(tzinfo=None))
    billable_impressions = sum(imp for date, (imp,) in imps)
    return billable_impressions


def get_billable_amount(camp, impressions):
    if hasattr(camp, 'cpm'):
        value_delivered = impressions / 1000. * camp.cpm / 100.
        billable_amount = min(camp.bid, value_delivered)
    else:
        # pre-CPM campaigns are charged in full regardless of impressions
        billable_amount = camp.bid

    billable_amount = Decimal(str(billable_amount)).quantize(Decimal('.01'),
                                                        rounding=ROUND_DOWN)
    return float(billable_amount)


def get_spent_amount(campaign):
    if hasattr(campaign, 'refund_amount'):
        # no need to calculate spend if we've already refunded
        spent = campaign.bid - campaign.refund_amount
    elif not hasattr(campaign, 'cpm'):
        # pre-CPM campaign
        return campaign.bid
    else:
        billable_impressions = get_billable_impressions(campaign)
        spent = get_billable_amount(campaign, billable_impressions)
    return spent


def successful_payment(link, campaign, ip, address):
    if not address:
        return

    campaign.trans_ip = ip
    campaign.trans_billing_country = address.country

    location = location_by_ips(ip)

    if location:
        campaign.trans_ip_country = location.get("country_name")

        countries_match = (campaign.trans_billing_country.lower() ==
            campaign.trans_ip_country.lower())
        campaign.trans_country_match = countries_match

    campaign._commit()


def new_payment_method(user, ip, address, link):
    user._incr('num_payment_methods')
    hooks.get_hook('promote.new_payment_method').call(user=user, ip=ip, address=address, link=link)


def failed_payment_method(user, link):
    user._incr('num_failed_payments')
    hooks.get_hook('promote.failed_payment').call(user=user, link=link)


def Run(verbose=True):
    """reddit-job-update_promos: Intended to be run hourly to pull in
    scheduled changes to ads

    """

    if verbose:
        print "%s promote.py:Run() - make_daily_promotions()" % datetime.datetime.now(g.tz)

    make_daily_promotions()

    if verbose:
        print "%s promote.py:Run() - finished" % datetime.datetime.now(g.tz)

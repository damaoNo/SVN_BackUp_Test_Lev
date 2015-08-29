--
--   Licensed to the Apache Software Foundation (ASF) under one or more
--   contributor license agreements.  See the NOTICE file distributed with
--   this work for additional information regarding copyright ownership.
--   The ASF licenses this file to You under the Apache License, Version 2.0
--   (the "License"); you may not use this file except in compliance with
--   the License.  You may obtain a copy of the License at
--
--      http://www.apache.org/licenses/LICENSE-2.0
--
--   Unless required by applicable law or agreed to in writing, software
--   distributed under the License is distributed on an "AS IS" BASIS,
--   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
--   See the License for the specific language governing permissions and
--   limitations under the License.
--
-- This tests for the fix of a bug in the readExternal method of
-- org.apache.derby.iapi.types.execution.SQLDecimal
-- (Track # 2834)

create table test_numeric (v numeric(31,28));

insert into test_numeric values(0.153659349252549204400963844818761572241783142089843750000000000000000);
insert into test_numeric values(0.155519172871977917615993192157475277781486511230468750000000000000000);
insert into test_numeric values(0.156845683349995823618883150629699230194091796875000000000000000000000);
insert into test_numeric values(0.167771342363935538344321685144677758216857910156250000000000000000000);
insert into test_numeric values(0.169551943407624583493031877878820523619651794433593750000000000000000);
insert into test_numeric values(0.171827807248857933331009917310439050197601318359375000000000000000000);
insert into test_numeric values(0.172725514170567251426291477400809526443481445312500000000000000000000);
insert into test_numeric values(0.173790440056551531711193092633038759231567382812500000000000000000000);
insert into test_numeric values(0.178284396304296932633803862700005993247032165527343750000000000000000);
insert into test_numeric values(0.190443865345212404172059450502274557948112487792968750000000000000000);
insert into test_numeric values(0.204708121258723396707068786781746894121170043945312500000000000000000);
insert into test_numeric values(0.204925853612388464419780120806535705924034118652343750000000000000000);
insert into test_numeric values(0.206782991393041903904759237775579094886779785156250000000000000000000);
insert into test_numeric values(0.208975811755244267331477203697431832551956176757812500000000000000000);
insert into test_numeric values(0.219113708053228051220173711044481024146080017089843750000000000000000);
insert into test_numeric values(0.219350734394552437933612054621335119009017944335937500000000000000000);
insert into test_numeric values(0.221012255176880945128914390807040035724639892578125000000000000000000);
insert into test_numeric values(0.233852021438996970914558914955705404281616210937500000000000000000000);
insert into test_numeric values(0.250165879091443721371490482852095738053321838378906250000000000000000);
insert into test_numeric values(0.254732077306483950529525372985517606139183044433593750000000000000000);
insert into test_numeric values(0.256652589621466642455516193876974284648895263671875000000000000000000);
insert into test_numeric values(0.259177521128625021340496914490358904004096984863281250000000000000000);
insert into test_numeric values(0.259711742936781675439306127373129129409790039062500000000000000000000);
insert into test_numeric values(0.263426083107227371193914677860448136925697326660156250000000000000000);
insert into test_numeric values(0.265153938284969137306745778914773836731910705566406250000000000000000);
insert into test_numeric values(0.284394754291467433127138519921572878956794738769531250000000000000000);
insert into test_numeric values(0.284501720963279192133654760255012661218643188476562500000000000000000);
insert into test_numeric values(0.285773541682349141446195517346495762467384338378906250000000000000000);
insert into test_numeric values(0.293670096014542747475672967993887141346931457519531250000000000000000);
insert into test_numeric values(0.298141579351063668035237697040429338812828063964843750000000000000000);
insert into test_numeric values(0.300830762733429235566973147797398269176483154296875000000000000000000);
insert into test_numeric values(0.301480275672052488999952402082271873950958251953125000000000000000000);
insert into test_numeric values(0.313410495834889335498019136139191687107086181640625000000000000000000);
insert into test_numeric values(0.315596999671068245696403664624085649847984313964843750000000000000000);
insert into test_numeric values(0.316767836466031571518442433443851768970489501953125000000000000000000);
insert into test_numeric values(0.319831561014056187097764905047370120882987976074218750000000000000000);
insert into test_numeric values(0.321581867590551295776890583510976284742355346679687500000000000000000);
insert into test_numeric values(0.330044919065760167242729039571713656187057495117187500000000000000000);
insert into test_numeric values(0.340170172948979998572838212567148730158805847167968750000000000000000);
insert into test_numeric values(0.343023330195411535470384478685446083545684814453125000000000000000000);
insert into test_numeric values(0.343528838909644917976038414053618907928466796875000000000000000000000);
insert into test_numeric values(0.348038324953031108499601486983010545372962951660156250000000000000000);
insert into test_numeric values(0.348176589940960012903303777420660480856895446777343750000000000000000);
insert into test_numeric values(0.348387561880196816588295405381359159946441650390625000000000000000000);
insert into test_numeric values(0.348957260513803158019641159626189619302749633789062500000000000000000);
insert into test_numeric values(0.353760949608404873245603994291741400957107543945312500000000000000000);
insert into test_numeric values(0.353904370909915555465374836785485967993736267089843750000000000000000);
insert into test_numeric values(0.355196672070496766160374590981518849730491638183593750000000000000000);
insert into test_numeric values(0.356598092009570155624942344729788601398468017578125000000000000000000);
insert into test_numeric values(0.361891550398754757722485919657628983259201049804687500000000000000000);
insert into test_numeric values(0.363017511046693774900973039621021598577499389648437500000000000000000);
insert into test_numeric values(0.364295947573769596239401380444178357720375061035156250000000000000000);
insert into test_numeric values(0.365173282132318477444243853824445977807044982910156250000000000000000);
insert into test_numeric values(0.366457422041641223131591686978936195373535156250000000000000000000000);
insert into test_numeric values(0.373406205230288557039841634832555428147315979003906250000000000000000);
insert into test_numeric values(0.379451243731913856471749113552505150437355041503906250000000000000000);
insert into test_numeric values(0.387571502139793078178797713917447254061698913574218750000000000000000);
insert into test_numeric values(0.391076141514143826860561148350825533270835876464843750000000000000000);
insert into test_numeric values(0.394079701321526587598498281295178458094596862792968750000000000000000);
insert into test_numeric values(0.408450075488608899121345530147664248943328857421875000000000000000000);
insert into test_numeric values(0.413805706051468047412811301910551264882087707519531250000000000000000);
insert into test_numeric values(0.418969557566636341405796883918810635805130004882812500000000000000000);
insert into test_numeric values(0.426813242129662429036329740483779460191726684570312500000000000000000);
insert into test_numeric values(0.437034717190214694326755306974519044160842895507812500000000000000000);
insert into test_numeric values(0.448146058677034719863740974687971174716949462890625000000000000000000);
insert into test_numeric values(0.448560403250551353870889670361066237092018127441406250000000000000000);
insert into test_numeric values(0.453820269616386329225576901080785319209098815917968750000000000000000);
insert into test_numeric values(0.456269809786531088668937172769801691174507141113281250000000000000000);
insert into test_numeric values(0.461855255922355767417286642739782109856605529785156250000000000000000);
insert into test_numeric values(0.469232123754527163939087586186360567808151245117187500000000000000000);
insert into test_numeric values(0.470105968957435504940178816468687728047370910644531250000000000000000);
insert into test_numeric values(0.471049530443553798875200300244614481925964355468750000000000000000000);
insert into test_numeric values(0.471801288954409425713265591184608638286590576171875000000000000000000);
insert into test_numeric values(0.472098174014232152551073795621050521731376647949218750000000000000000);
insert into test_numeric values(0.479998044300088966274131507816491648554801940917968750000000000000000);
insert into test_numeric values(0.487510348557400718938481531949946656823158264160156250000000000000000);
insert into test_numeric values(0.488344394316039820402863824710948392748832702636718750000000000000000);
insert into test_numeric values(0.491453931889371697927515469928039237856864929199218750000000000000000);
insert into test_numeric values(0.492811951393927194509103628661250695586204528808593750000000000000000);
insert into test_numeric values(0.498686934685222960084161059057805687189102172851562500000000000000000);
insert into test_numeric values(0.508551183317009658502172442240407690405845642089843750000000000000000);
insert into test_numeric values(0.510021698576308746275742578291101381182670593261718750000000000000000);
insert into test_numeric values(0.514641581305544493218917523336131125688552856445312500000000000000000);
insert into test_numeric values(0.514838599259439644306723948830040171742439270019531250000000000000000);
insert into test_numeric values(0.516254456787693660757554425799753516912460327148437500000000000000000);
insert into test_numeric values(0.517424998087975929195181379327550530433654785156250000000000000000000);
insert into test_numeric values(0.520617556348049959069612668827176094055175781250000000000000000000000);
insert into test_numeric values(0.523540519518414626531921385321766138076782226562500000000000000000000);
insert into test_numeric values(0.526207255428038367384147022676188498735427856445312500000000000000000);
insert into test_numeric values(0.536035818449368539617694295884575694799423217773437500000000000000000);
insert into test_numeric values(0.538519356120012004929265003738692030310630798339843750000000000000000);
insert into test_numeric values(0.539919567857022331125449454702902585268020629882812500000000000000000);
insert into test_numeric values(0.546738509435749198139831150911049917340278625488281250000000000000000);
insert into test_numeric values(0.555397163378280045442636492225574329495429992675781250000000000000000);
insert into test_numeric values(0.556609269078325530344386606884654611349105834960937500000000000000000);
insert into test_numeric values(0.559546306378952973403784199035726487636566162109375000000000000000000);
insert into test_numeric values(0.562659733610342049914265771803911775350570678710937500000000000000000);
insert into test_numeric values(0.563120243089950678339050682552624493837356567382812500000000000000000);
insert into test_numeric values(0.564708580310341101693438758957199752330780029296875000000000000000000);
insert into test_numeric values(0.575994406119186841408463806146755814552307128906250000000000000000000);
insert into test_numeric values(0.577354105251341342963655733910854905843734741210937500000000000000000);
insert into test_numeric values(0.580863074718258265427550668391631916165351867675781250000000000000000);
insert into test_numeric values(0.582986835256425472984176394675159826874732971191406250000000000000000);
insert into test_numeric values(0.591042132949047616108373404131270945072174072265625000000000000000000);
insert into test_numeric values(0.598743143052751891630691716272849589586257934570312500000000000000000);
insert into test_numeric values(0.600505989779717141985315720376092940568923950195312500000000000000000);
insert into test_numeric values(0.617877239310097192692694534343900159001350402832031250000000000000000);
insert into test_numeric values(0.622180218573680554605687120783841237425804138183593750000000000000000);
insert into test_numeric values(0.627353931494473160412894685578066855669021606445312500000000000000000);
insert into test_numeric values(0.640540141483764813301604590378701686859130859375000000000000000000000);
insert into test_numeric values(0.643952050290426791789855087699834257364273071289062500000000000000000);
insert into test_numeric values(0.645499424366119400886532275762874633073806762695312500000000000000000);
insert into test_numeric values(0.650971601226496998648940461862366646528244018554687500000000000000000);
insert into test_numeric values(0.651357394998915650852211456367513164877891540527343750000000000000000);
insert into test_numeric values(0.656225638072990613558488348644459620118141174316406250000000000000000);
insert into test_numeric values(0.658941700667530216861678127315826714038848876953125000000000000000000);
insert into test_numeric values(0.661191909165676716675363877584459260106086730957031250000000000000000);
insert into test_numeric values(0.666205794165350417834758900426095351576805114746093750000000000000000);
insert into test_numeric values(0.666413162385818269584092377044726163148880004882812500000000000000000);

select v 
from test_numeric
where v < 0.55
order by v;

drop table test_numeric;

exit;



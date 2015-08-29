/*

   Derby - Class org.apache.derby.impl.services.uuid.BasicUUIDFactory

   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to you under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

 */

package org.apache.derby.impl.services.uuid;

import java.security.AccessController;
import java.security.PrivilegedAction;

import org.apache.derby.iapi.services.monitor.ModuleFactory;
import org.apache.derby.iapi.services.monitor.Monitor;
import org.apache.derby.catalog.UUID;
import org.apache.derby.iapi.services.uuid.UUIDFactory;

/**

  A hack implementation of something similar to a DCE UUID 
  generator.  Generates unique 128-bit numbers based on the
  current machine's internet address, the current time, and
  a sequence number.  This implementation should be made to
  conform to the DCE specification. ("DEC/HP, Network Computing
  Architecture, Remote Procedure Call Runtime Extensions
  Specification, version OSF TX1.0.11," Steven Miller, July
  23, 1992.  This is part of the OSF DCE Documentation.
  Chapter 10 describes the UUID generation algorithm.)
  <P>
  Some known deficiencies:
  <ul>
  <li> Rather than using the 48-bit hardware network address,
  it uses the 32-bit IP address. IP addresses are not
  guaranteed to be unique.
  <li> There is no provision for generating a suitably unique
  number if no IP address is available.
  <li> Two processes running on this machine which start their
  respective UUID services within a millisecond of one another
  may generate duplicate UUIDS.
  </ul>
  <P>
  However, the intention is that UUIDs generated from this class
  will be unique with respect to UUIDs generated by other DCE
  UUID generators.

**/

public final class BasicUUIDFactory
	implements UUIDFactory
{
    /*
	** Fields of BasicUUIDFactory.
	*/

	private long majorId;  // 48 bits only
	private long timemillis;

	public BasicUUIDFactory() {
		Object env = getMonitor().getEnvironment();
		if (env != null) {
			String s = env.toString();
			if (s != null)
				env = s;

			majorId = ((long) env.hashCode());

			
		} else {
			majorId = Runtime.getRuntime().freeMemory();
		}

		majorId &= 0x0000ffffffffffffL;
		resetCounters();
	}


	//
	//	Constants and fields for computing the sequence number. We started out with monotonically
	//	increasing sequence numbers but realized that this causes collisions at the
	//	ends of BTREEs built on UUID columns. So now we have a random number
	//	generator. We generate these numbers using a technique from Knuth
	//	"Seminumerical Algorithms," section 3.2 (Generating Uniform Random Numbers).
	//	The formula is:
	//
	//		next = ( (MULTIPLIER * current) + STEP ) % MODULUS
	//
	//	Here
	//
	//		MODULUS			=	int size.
	//		MULTIPLIER		=	fairly close to the square root of MODULUS to force the
	//							sequence number to jump around. satisifies the rule that
	//							(MULTIPLIER-1) is divisible by 4 and by all the primes which
	//							divide MODULUS.
	//		STEP			=	a large number that keeps the sequence number jumping around.
	//							must be relatively prime to MODULUS.
	//		INITIAL_VALUE	=	a number guaranteeing that the first couple sequence numbers
	//							won't be monotonically increasing.
	//
	//	The sequence numbers should jump around and cycle through all numbers which fit in an int.

	private	static	final	long	MODULUS			=	( 1L << 32 );
	private	static	final	long	MULTIPLIER		=	( ( 1L << 14 ) + 1 );
	private	static	final	long	STEP			=	( ( 1L << 27 ) + 1 );
	private	static	final	long	INITIAL_VALUE	=	( 2551218188L );

	private			long	currentValue;

	/*
	** Methods of UUID
	*/

	/**
		Generate a new UUID.
		@see UUIDFactory#createUUID
	**/
	public synchronized UUID createUUID()
	{
		long cv = currentValue = ( ( MULTIPLIER * currentValue ) + STEP ) % MODULUS;
		if ( cv == INITIAL_VALUE ) { bumpMajor(); }
		int sequence = (int) cv;

		return new BasicUUID(majorId, timemillis, sequence);
	}

	/**
		Recreate a UUID previously generated UUID value.
		@see UUIDFactory#recreateUUID
	**/
	public UUID recreateUUID(String uuidstring)
	{
		return new BasicUUID(uuidstring);
	}

	private void bumpMajor() {

		// 48 bits only
		majorId = (majorId + 1L) & 0x0000ffffffffffffL;
		if (majorId == 0L)
			resetCounters();

	}
	private void resetCounters()
	{
		timemillis = System.currentTimeMillis();
		currentValue = INITIAL_VALUE;
	}
    
    /**
     * Privileged Monitor lookup. Must be private so that user code
     * can't call this entry point.
     */
    private  static  ModuleFactory  getMonitor()
    {
        return AccessController.doPrivileged
            (
             new PrivilegedAction<ModuleFactory>()
             {
                 public ModuleFactory run()
                 {
                     return Monitor.getMonitor();
                 }
             }
             );
    }

}


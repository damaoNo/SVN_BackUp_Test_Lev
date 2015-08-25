/*
 *  Licensed to the Apache Software Foundation (ASF) under one
 *  or more contributor license agreements.  See the NOTICE file
 *  distributed with this work for additional information
 *  regarding copyright ownership.  The ASF licenses this file
 *  to you under the Apache License, Version 2.0 (the
 *  "License"); you may not use this file except in compliance
 *  with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing,
 *  software distributed under the License is distributed on an
 *   * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 *  KIND, either express or implied.  See the License for the
 *  specific language governing permissions and limitations
 *  under the License.
 */

package org.apache.axis2.transport.testkit.filter;

import java.util.Map;

/**
 * Implementation of the <em>or</em> (<tt>|</tt>) operator.
 */
public class OrExpression implements FilterExpression {
    private final FilterExpression[] operands;

    public OrExpression(FilterExpression[] operands) {
        this.operands = operands;
    }

    public boolean matches(Map<String,String> map) {
        for (FilterExpression operand : operands) {
            if (operand.matches(map)) {
                return true;
            }
        }
        return false;
    }
}

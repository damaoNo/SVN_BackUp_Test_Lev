/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package java.sql;

import java.io.InputStream;
import java.io.Reader;
import java.math.BigDecimal;
import java.net.URL;
import java.util.Map;
import java.util.Properties;

public  interface   Connection  extends Wrapper
{
    int TRANSACTION_NONE	     = 0;
    int TRANSACTION_READ_COMMITTED   = 2;
    int TRANSACTION_READ_UNCOMMITTED = 1;
    int TRANSACTION_REPEATABLE_READ  = 4;
    int TRANSACTION_SERIALIZABLE     = 8;

    public  void 	clearWarnings() throws  SQLException;
    public  void 	close() throws  SQLException;
    public  void 	commit()    throws  SQLException;
    public  Array 	createArrayOf(String typeName, Object[] elements)   throws  SQLException;
    public  Blob 	createBlob()    throws  SQLException;
    public  Clob 	createClob()    throws  SQLException;
    public  NClob 	createNClob()   throws  SQLException;
    public  SQLXML 	createSQLXML()  throws  SQLException;
    public  Statement 	createStatement()   throws  SQLException;
    public  Statement 	createStatement(int resultSetType, int resultSetConcurrency)    throws  SQLException;
    public  Statement 	createStatement(int resultSetType, int resultSetConcurrency, int resultSetHoldability)  throws  SQLException;
    public  Struct 	createStruct(String typeName, Object[] attributes)  throws  SQLException;
    public  boolean 	getAutoCommit() throws  SQLException;
    public  String 	getCatalog()    throws  SQLException;
    public  Properties 	getClientInfo() throws  SQLException;
    public  String 	getClientInfo(String name)  throws  SQLException;
    public  int 	getHoldability()    throws  SQLException;
    public  DatabaseMetaData 	getMetaData()   throws  SQLException;
    public  int 	getTransactionIsolation()   throws  SQLException;
    public  Map<String,Class<?>> 	getTypeMap()    throws  SQLException;
    public  SQLWarning 	getWarnings()   throws  SQLException;
    public  boolean 	isClosed()  throws  SQLException;
    public  boolean 	isReadOnly()    throws  SQLException;
    public  boolean 	isValid(int timeout)    throws  SQLException;
    public  String 	nativeSQL(String sql)   throws  SQLException;
    public  CallableStatement 	prepareCall(String sql) throws  SQLException;
    public  CallableStatement 	prepareCall(String sql, int resultSetType, int resultSetConcurrency)    throws  SQLException;
    public  CallableStatement 	prepareCall(String sql, int resultSetType, int resultSetConcurrency, int resultSetHoldability)  throws  SQLException;
    public  PreparedStatement 	prepareStatement(String sql)    throws  SQLException;
    public  PreparedStatement 	prepareStatement(String sql, int autoGeneratedKeys) throws  SQLException;
    public  PreparedStatement 	prepareStatement(String sql, int[] columnIndexes)   throws  SQLException;
    public  PreparedStatement 	prepareStatement(String sql, int resultSetType, int resultSetConcurrency)   throws  SQLException;
    public  PreparedStatement 	prepareStatement(String sql, int resultSetType, int resultSetConcurrency, int resultSetHoldability) throws  SQLException;
    public  PreparedStatement 	prepareStatement(String sql, String[] columnNames)  throws  SQLException;
    public  void 	releaseSavepoint(Savepoint savepoint)   throws  SQLException;
    public  void 	rollback()  throws  SQLException;
    public  void 	rollback(Savepoint savepoint)   throws  SQLException;
    public  void 	setAutoCommit(boolean autoCommit)   throws  SQLException;
    public  void 	setCatalog(String catalog)  throws  SQLException;
    public  void 	setClientInfo(Properties properties)    throws  SQLException;
    public  void 	setClientInfo(String name, String value)    throws  SQLException;
    public  void 	setHoldability(int holdability) throws  SQLException;
    public  void 	setReadOnly(boolean readOnly)   throws  SQLException;
    public  Savepoint 	setSavepoint()  throws  SQLException;
    public  Savepoint 	setSavepoint(String name)   throws  SQLException;
    public  void 	setTransactionIsolation(int level)  throws  SQLException;
    public  void 	setTypeMap(Map<String,Class<?>> map)    throws  SQLException;
}

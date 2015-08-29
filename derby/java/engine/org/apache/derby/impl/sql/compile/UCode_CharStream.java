/*

   Derby - Class org.apache.derby.impl.sql.compile.UCode_CharStream

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

/* Generated By:JavaCC: Do not edit this line. UCode_CharStream.java Version 0.7pre6 */
package org.apache.derby.impl.sql.compile;

/**
 * An implementation of interface CharStream, where the stream is assumed to
 * contain only Unicode characters.
 */

// NOTE: This class was modified to support the ability to get all the
// characters in the input stream between two tokens.  - Jeff

public final class UCode_CharStream implements CharStream
{
  // The next two lines are added to support ability to get the input
  // between two tokens.
  int charCnt;
  int[] charOffset;

  public static final boolean staticFlag = false;
  public int bufpos = -1;
  int bufsize;
  int available;
  int tokenBegin;
  private int bufline[];
  private int bufcolumn[];

  private int column = 0;
  private int line = 1;

  private boolean prevCharIsCR = false;
  private boolean prevCharIsLF = false;

  private java.io.Reader inputStream;

  private char[] nextCharBuf;
  private char[] buffer;
  private int maxNextCharInd = 0;
  private int nextCharInd = -1;

  private void ExpandBuff(boolean wrapAround)
  {
     char[] newbuffer = new char[bufsize + 2048];
     int newbufline[] = new int[bufsize + 2048];
     int newbufcolumn[] = new int[bufsize + 2048];

	// The next line was added to support ability to get the input
	// between two tokens.
	int newcharOffset[] = new int[bufsize + 2048];

     try
     {
        if (wrapAround)
        {
           System.arraycopy(buffer, tokenBegin, newbuffer, 0, bufsize - tokenBegin);
           System.arraycopy(buffer, 0, newbuffer,
                                             bufsize - tokenBegin, bufpos);
           buffer = newbuffer;

           System.arraycopy(bufline, tokenBegin, newbufline, 0, bufsize - tokenBegin);
           System.arraycopy(bufline, 0, newbufline, bufsize - tokenBegin, bufpos);
           bufline = newbufline;

           System.arraycopy(bufcolumn, tokenBegin, newbufcolumn, 0, bufsize - tokenBegin);
           System.arraycopy(bufcolumn, 0, newbufcolumn, bufsize - tokenBegin, bufpos);
           bufcolumn = newbufcolumn;

			// The next three lines were added to support ability to get input
			// between two tokens.
		   System.arraycopy(charOffset, tokenBegin, newcharOffset, 0, bufsize - tokenBegin);
		   System.arraycopy(charOffset, 0, newcharOffset, bufsize - tokenBegin, bufpos);
		   charOffset = newcharOffset;

           bufpos += (bufsize - tokenBegin);
        }
        else
        {
           System.arraycopy(buffer, tokenBegin, newbuffer, 0, bufsize - tokenBegin);
           buffer = newbuffer;

           System.arraycopy(bufline, tokenBegin, newbufline, 0, bufsize - tokenBegin);
           bufline = newbufline;

           System.arraycopy(bufcolumn, tokenBegin, newbufcolumn, 0, bufsize - tokenBegin);
           bufcolumn = newbufcolumn;

			// The next two lines were added to support ability to get input
			// between two tokens.
		   System.arraycopy(charOffset, tokenBegin, newcharOffset, 0, bufsize - tokenBegin);
		   charOffset = newcharOffset;

           bufpos -= tokenBegin;
        }
     }
     catch (Throwable t)
     {
        throw new Error(t.getMessage());
     }

     available = (bufsize += 2048);
     tokenBegin = 0;
  }

  private void FillBuff() throws java.io.IOException
  {
     if (maxNextCharInd == nextCharBuf.length)
        maxNextCharInd = nextCharInd = 0;

     int i;
     try {
        if ((i = inputStream.read(nextCharBuf, maxNextCharInd,
                                  nextCharBuf.length - maxNextCharInd)) == -1)
        {
           inputStream.close();
           throw new java.io.IOException();
        }
        else
           maxNextCharInd += i;
     }
     catch(java.io.IOException e) {
        if (bufpos != 0)
        {
           --bufpos;
           backup(0);
        }
        else
        {
           bufline[bufpos] = line;
           bufcolumn[bufpos] = column;
        }
        if (tokenBegin == -1)
           tokenBegin = bufpos;
        throw e;
     }
  }

  private char ReadChar() throws java.io.IOException
  {
     if (++nextCharInd >= maxNextCharInd)
        FillBuff();

	 return nextCharBuf[nextCharInd];
  }
     
  public char BeginToken() throws java.io.IOException
  {     
	 if (inBuf > 0)
	 {
		--inBuf;
		return buffer[tokenBegin = (bufpos == bufsize - 1) ? (bufpos = 0)
															: ++bufpos];
	 }

     tokenBegin = 0;
	 bufpos = -1;
     char c = readChar();

     return c;
  }     

  private void UpdateLineColumn(char c)
  {
     column++;

     if (prevCharIsLF)
     {
        prevCharIsLF = false;
        line += (column = 1);
     }
     else if (prevCharIsCR)
     {
        prevCharIsCR = false;
        if (c == '\n')
        {
           prevCharIsLF = true;
        }
        else
           line += (column = 1);
     }

     switch (c)
     {
        case '\r' :
           prevCharIsCR = true;
           break;
        case '\n' :
           prevCharIsLF = true;
           break;
        case '\t' :
           column--;
           column += (8 - (column & 07));
           break;
        default :
           break;
     }

	 bufline[bufpos] = line;
	 bufcolumn[bufpos] = column;
  }

  private int inBuf = 0;
  public final char readChar() throws java.io.IOException
  {
     if (inBuf > 0)
     {
        --inBuf;
        return buffer[(bufpos == bufsize - 1) ? (bufpos = 0) : ++bufpos];
     }

     if (++bufpos == available)
     {
        if (available == bufsize)
        {
           if (tokenBegin > 2048)
           {
              bufpos = 0;
              available = tokenBegin;
           }
           else if (tokenBegin < 0)
              bufpos = 0;
           else
              ExpandBuff(false);
        }
        else if (available > tokenBegin)
           available = bufsize;
        else if ((tokenBegin - available) < 2048)
           ExpandBuff(true);
        else
           available = tokenBegin;
     }

	 char c = ReadChar();

     UpdateLineColumn(c);

	// The next line was added to support ability to get the input
	// between two tokens.
	charOffset[bufpos] = charCnt++;

     return (buffer[bufpos] = c);
  }

  /**
   * @deprecated 
   * @see #getEndColumn
   */
  @Deprecated
  public final int getColumn() {
     return bufcolumn[bufpos];
  }

  /**
   * @deprecated 
   * @see #getEndLine
   */
  @Deprecated
  public final int getLine() {
     return bufline[bufpos];
  }

  public final int getEndColumn() {
     return bufcolumn[bufpos];
  }

  public final int getEndLine() {
     return bufline[bufpos];
  }

  public final int getBeginColumn() {
     return bufcolumn[tokenBegin];
  }

  public final int getBeginLine() {
     return bufline[tokenBegin];
  }

  // This method was added to support ability to get the input
  // between two tokens.
  public final int getBeginOffset() {
	return charOffset[tokenBegin];
  }

  // This method was added to support ability to get the input
  // between two tokens.
  public final int getEndOffset() {
	return charOffset[bufpos];
  }

  public final void backup(int amount) {

    inBuf += amount;
    if ((bufpos -= amount) < 0)
       bufpos += bufsize;
  }

  public UCode_CharStream(java.io.Reader dstream,
                 int startline, int startcolumn, int buffersize)
  {
    inputStream = dstream;
    line = startline;
    column = startcolumn - 1;

    available = bufsize = buffersize;
    buffer = new char[buffersize];
    nextCharBuf = new char[buffersize];
    bufline = new int[buffersize];
    bufcolumn = new int[buffersize];

	// The next line was added to support ability to get the input
	// between two tokens.
	charOffset = new int[buffersize];
  }

  public UCode_CharStream(java.io.Reader dstream,
                                        int startline, int startcolumn)
  {
     this(dstream, startline, startcolumn, 4096);
  }

  public void ReInit(java.io.Reader dstream,
                 int startline, int startcolumn, int buffersize)
  {
    inputStream = dstream;
    line = startline;
    column = startcolumn - 1;

    if (buffer == null || buffersize != buffer.length)
    {
      available = bufsize = buffersize;
      buffer = new char[buffersize];
      nextCharBuf = new char[buffersize];
      bufline = new int[buffersize];
      bufcolumn = new int[buffersize];
    }

	// The next line was added to support ability to get the input
	// between two tokens.
	inBuf = maxNextCharInd = charCnt = tokenBegin = 0;
	nextCharInd = bufpos = -1;
  }

  public void ReInit(java.io.Reader dstream,
                                        int startline, int startcolumn)
  {
     ReInit(dstream, startline, startcolumn, 4096);
  }
  public UCode_CharStream(java.io.InputStream dstream, int startline,
  int startcolumn, int buffersize)
  {
     this(new java.io.InputStreamReader(dstream), startline, startcolumn, 4096);
  }

  public UCode_CharStream(java.io.InputStream dstream, int startline,
                                                           int startcolumn)
  {
     this(dstream, startline, startcolumn, 4096);
  }

  public void ReInit(java.io.InputStream dstream, int startline,
  int startcolumn, int buffersize)
  {
     ReInit(new java.io.InputStreamReader(dstream), startline, startcolumn, 4096);
  }
  public void ReInit(java.io.InputStream dstream, int startline,
                                                           int startcolumn)
  {
     ReInit(dstream, startline, startcolumn, 4096);
  }

  public final String GetImage()
  {
     if (bufpos >= tokenBegin)
        return new String(buffer, tokenBegin, bufpos - tokenBegin + 1);
     else
        return new String(buffer, tokenBegin, bufsize - tokenBegin) +
                              new String(buffer, 0, bufpos + 1);
  }

  public final char[] GetSuffix(int len)
  {
     char[] ret = new char[len];

     if ((bufpos + 1) >= len)
        System.arraycopy(buffer, bufpos - len + 1, ret, 0, len);
     else
     {
        System.arraycopy(buffer, bufsize - (len - bufpos - 1), ret, 0,
                                                          len - bufpos - 1);
        System.arraycopy(buffer, 0, ret, len - bufpos - 1, bufpos + 1);
     }

     return ret;
  }

  public void Done()
  {
     nextCharBuf = null;
     buffer = null;
     bufline = null;
     bufcolumn = null;

	// The next line was added to support ability to get the input
	// between two tokens.
	 charOffset = null;
  }

  /**
   * Method to adjust line and column numbers for the start of a token.<BR>
   */
  public void adjustBeginLineColumn(int newLine, int newCol)
  {
     int start = tokenBegin;
     int len;

     if (bufpos >= tokenBegin)
     {
        len = bufpos - tokenBegin + inBuf + 1;
     }
     else
     {
        len = bufsize - tokenBegin + bufpos + 1 + inBuf;
     }

     int i = 0, j = 0, k = 0;
     int columnDiff = 0;

     while (i < len &&
            bufline[j = start % bufsize] == bufline[k = ++start % bufsize])
     {
        bufline[j] = newLine;
        int nextColDiff = columnDiff + bufcolumn[k] - bufcolumn[j];
        bufcolumn[j] = newCol + columnDiff;
        columnDiff = nextColDiff;
        i++;
     } 

     if (i < len)
     {
        bufline[j] = newLine++;
        bufcolumn[j] = newCol + columnDiff;

        while (i++ < len)
        {
           if (bufline[j = start % bufsize] != bufline[++start % bufsize])
              bufline[j] = newLine++;
           else
              bufline[j] = newLine;
        }
     }

     line = bufline[j];
     column = bufcolumn[j];
  }

}

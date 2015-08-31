// $Id: ILogControl.cs 11403 2012-07-19 12:59:14Z rhuijben $
//
// Copyright 2008 The AnkhSVN Project
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Ankh.Scc.UI
{
    public interface ILogControl
    {
        bool ShowChangedPaths { get; set; }
        bool ShowLogMessage { get; set; }
        bool StopOnCopy { get; set; }
        bool IncludeMergedRevisions { get; set; }
        void FetchAll();
        void Restart();

        IList<SvnOrigin> Origins { get; }        
    }
}

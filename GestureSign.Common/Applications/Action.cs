﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using GestureSign.Common.Applications;

namespace GestureSign.Applications
{
    [DataContract]
    public class Action : IAction
    {
        #region Public Properties

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string PluginFilename { get; set; }

        [DataMember]
        public string PluginClass { get; set; }

        [DataMember]
        public string GestureName { get; set; }

        [DataMember]
        public string ActionSettings { get; set; }

        [DataMember]
        public bool IsEnabled { get; set; }

        [DataMember]
        public string Condition { get; set; }
        #endregion
    }
}

﻿using System;
using System.Collections.Generic;

namespace Horizon.Database.Entities
{
    public partial class DimAppIds
    {
        public int AppId { get; set; }
        public string AppName { get; set; }
        public int? GroupId { get;set;}
    }
}

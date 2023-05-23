﻿using System;

namespace Horizon.Database.Entities
{
    public class PostDebugInfo
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public int AppId { get; set; }
        public DateTime CreateDt { get; set; }
    }
}
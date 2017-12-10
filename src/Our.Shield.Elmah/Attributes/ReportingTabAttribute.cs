﻿using Our.Shield.Core.Attributes;

namespace Our.Shield.Elmah.Attributes
{
    public class ReportingTabAttribute : AppTabAttribute
    {
        public ReportingTabAttribute() : base("Reporting", 0, "") { }

        public ReportingTabAttribute(string caption, int sortOrder, string filePath) : base(caption, sortOrder, filePath)
        {
        }
    }
}
﻿
using System;
using System.Collections.Generic;
using System.Text;
using MathNet.Numerics;
using OxyPlot;

namespace MSFileInfoScanner
{
    class clsAxisInfo
    {
        public const string DEFAULT_AXIS_LABEL_FORMAT = "#,##0";

        public const string EXPONENTIAL_FORMAT = "0.00E+00";

        public bool AutoScale { get; set; }

        public double Maximum { get; set; }

        public double Minimum { get; set; }

        public double MajorStep { get; set; }

        public double MinorGridlineThickness { get; set; }

        public string StringFormat { get; set; }

        public string Title { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsAxisInfo(string title = "Undefined")
        {
            AutoScale = true;
            MajorStep = 1;
            MinorGridlineThickness = 0;
            StringFormat = DEFAULT_AXIS_LABEL_FORMAT;
            Title = title;
        }

        /// <summary>
        /// Get options as a semi colon separated list of key-value pairs
        /// </summary>
        /// <returns></returns>
        public string GetOptions()
        {
            return GetOptions(new List<string>());
        }

        /// <summary>
        /// Get options as a semi colon separated list of key-value pairs
        /// </summary>
        /// <returns></returns>
        public string GetOptions(List<string> additionalOptions)
        {
            var options = new List<string>();

            if (AutoScale)
            {
                options.Add("Autoscale=true");
            }
            else
            {
                options.Add("Autoscale=false");
                options.Add("Minimum=" + Minimum);
                options.Add("Maximum=" + Maximum);
            }

            options.Add("StringFormat=" + StringFormat);
            options.Add("MinorGridlineThickness=" + MinorGridlineThickness);
            options.Add("MajorStep=" + MajorStep);

            if (additionalOptions != null && additionalOptions.Count > 0)
                options.AddRange(additionalOptions);

            return string.Join(";", options);
        }

        /// <summary>
        /// Set the axis range
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <remarks>Set min and max to 0 to enable autoscaling</remarks>
        public void SetRange(double min, double max)
        {
            if (Math.Abs(min) < float.Epsilon && Math.Abs(max) < float.Epsilon)
            {
                AutoScale = true;
                return;
            }

            AutoScale = false;

            Minimum = min;
            Maximum = max;

        }

    }
}

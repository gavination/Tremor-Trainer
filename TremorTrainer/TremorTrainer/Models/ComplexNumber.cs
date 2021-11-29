using System;
using System.Collections.Generic;
using System.Text;

namespace TremorTrainer.Models
{
    public class ComplexNumber
    {
        public float Magnitude { get; set; }
        public float Phase { get; set; }
        public float Imaginary { get; set; }
        public float Real { get; set; }
        public float MagnitudeSquared { get; set; }
        public ComplexNumber Sign { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Output
{
    public class NestResults : Results
    {
        public NestResults(string experiment)
            :base(experiment + "_colony")
        {

        }
    }
}

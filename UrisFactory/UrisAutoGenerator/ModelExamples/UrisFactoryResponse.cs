﻿using Swashbuckle.AspNetCore.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UrisFactory.ModelExamples
{
    public class UrisFactoryResponse: IExamplesProvider<string>
    {
        public string GetExamples()
        {
            return "http://data.um.es/res/researcher/121s";
        }
 
    }
}

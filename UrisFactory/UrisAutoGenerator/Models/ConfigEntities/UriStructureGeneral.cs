﻿using System.Collections.Generic;

namespace UrisFactory.Models.ConfigEntities
{
    ///<summary>
    ///Objeto que simula el fichero Json de configuración
    ///</summary>
    public class UriStructureGeneral
    {
        public string Base { get; set; }
        public IList<Characters> Characters { get; set; }
        public IList<UriStructure> UriStructures { get; set; }
        public IList<ResourcesClass> ResourcesClasses { get; set; }
    }
}

﻿using ApiCargaWebInterface.ViewModels;
using System;
using System.Collections.Generic;

namespace ApiCargaWebInterface.Models.Services
{
    public interface ICallShapeConfigService
    {
        public List<ShapeConfigViewModel> GetShapeConfigs();
        public ShapeConfigViewModel GetShapeConfig(Guid id);
        public bool DeleteShapeConfig(Guid id);
        public ShapeConfigViewModel CreateShapeConfig(ShapeConfigCreateModel newRepositoryConfigView);
        public void ModifyShapeConfig(ShapeConfigEditModel repositoryConfigView);
    }
}

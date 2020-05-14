﻿using System;
using System.Collections.Generic;
using API_CARGA.ModelExamples;
using API_CARGA.Models.Entities;
using API_CARGA.Models.Services;
using API_CARGA.Models.Utility;
using API_CARGA.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using VDS.RDF;

namespace API_CARGA.Controllers
{
    [Route("etl-config/[controller]")]
    [ApiController]
    public class ValidationController : ControllerBase
    {
        IShapesConfigService _shapeConfigService;
        public ValidationController(IShapesConfigService iShapeConfigService)
        {
            _shapeConfigService = iShapeConfigService;
        }
        /// <summary>
        /// Obtiene la configuración de los shape SHACL de validación
        /// </summary>
        /// <returns>Listado con las definiciones de las validaciones</returns>       
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status200OK, "Example", typeof(List<ShapeConfig>))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ShapesConfigsResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetShape()
        {
            return Ok(_shapeConfigService.GetShapesConfigs());
        }

        /// <summary>
        /// Obtiene la configuración del shape SHACL pasado por parámetro
        /// </summary>
        /// <param name="identifier">Identificador de la validación</param>
        /// <returns>Definición de la validación</returns>       
        [HttpGet("{identifier}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status200OK, "Example", typeof(ShapeConfig))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ShapeConfigResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetShape(Guid identifier)
        {
            return Ok(_shapeConfigService.GetShapeConfigById(identifier));
        }

        /// <summary>
        /// Añade una configuración de validación mediante un shape SHACL
        /// </summary>
        /// <param name="name">Nombre del Shape</param>
        /// <param name="repositoryID">ID del repositorio de la validación, este parametro se puede obtener con el método http://herc-as-front-desa.atica.um.es/carga/etl-config/Repository</param>
        /// <param name="rdfFile">Fichero con el Shape</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status200OK, "Example", typeof(Guid))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Example", typeof(ErrorExample))]
        [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(AddShapeConfigErrorResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult AddShape(string name, Guid repositoryID, IFormFile rdfFile)
        {
            ShapeConfig shapeconfig = new ShapeConfig();
            shapeconfig.ShapeConfigID = Guid.NewGuid();
            shapeconfig.Name = name;
            shapeconfig.RepositoryID = repositoryID;
            try
            {
                shapeconfig.Shape = SparqlUtility.GetTextFromFile(rdfFile);
                IGraph shapeGraph = new Graph();
                shapeGraph.LoadFromString(shapeconfig.Shape);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }

            try
            {
                Guid addedID = _shapeConfigService.AddShapeConfig(shapeconfig);
                if (!addedID.Equals(Guid.Empty))
                {
                    return Ok(addedID);
                }
                else
                {
                    return Problem($"Se ha producido un error al añadir el Shape");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorExample { Error = $"Se ha producido un error al añadir el Shape " + ex.Message });
            }            
        }

        /// <summary>
        /// Elimina la configuración una configuración de validación 
        /// </summary>
        /// <param name="identifier">Identificador de la validación</param>
        /// <returns></returns>
        [HttpDelete("{identifier}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult DeleteShape(Guid identifier)
        {
            bool deleted = _shapeConfigService.RemoveShapeConfig(identifier);
            if (deleted)
            {
                return Ok($"El shape con id {identifier} ha sido eliminado");
            }
            else
            {
                return Problem("Se ha producido un error al eliminar el Shape");
            }
        }

        /// <summary>
        /// Modifica la configuración de validación mediante un shape SHACL
        /// </summary>
        /// <param name="shapeConfigID">Identificador del Shape</param>
        /// <param name="name">Nombre del Shape</param>
        /// <param name="repositoryID">ID del repositorio de la validación, este parametro se puede obtener con el método http://herc-as-front-desa.atica.um.es/carga/etl-config/Repository</param>
        /// <param name="rdfFile">Fichero con el Shape</param>
        /// <returns></returns>
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Example", typeof(ErrorExample))]
        [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ModifyShapeConfigErrorResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ModifyShape(Guid shapeConfigID, string name, Guid repositoryID, IFormFile rdfFile)
        {
            ShapeConfig shapeconfig = new ShapeConfig();
            shapeconfig.ShapeConfigID = shapeConfigID;
            shapeconfig.Name = name;
            shapeconfig.RepositoryID = repositoryID;
            try
            {
                if (rdfFile != null) 
                {
                    shapeconfig.Shape = SparqlUtility.GetTextFromFile(rdfFile);
                    IGraph shapeGraph = new Graph();
                    shapeGraph.LoadFromString(shapeconfig.Shape);
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
            if (!_shapeConfigService.GetShapesConfigs().Exists(x => x.ShapeConfigID == shapeConfigID))
            {
                return BadRequest(new ErrorExample { Error = $"Comprueba si el shape config con id {shapeconfig.ShapeConfigID} existe" });
            }
            try
            {
                bool modified = _shapeConfigService.ModifyShapeConfig(shapeconfig);
                if (modified)
                {
                    return Ok($"La configuración del shape {shapeconfig.ShapeConfigID} ha sido modificada");
                }
                else
                {
                    return BadRequest(new ErrorExample { Error = $"Se ha producido un error al modificar el Shape" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorExample { Error = $"Se ha producido un error al modificar el Shape " + ex.Message });
            }

        }
    }
}
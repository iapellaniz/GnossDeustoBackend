﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CronConfigure.Models;
using CronConfigure.Models.Services;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CronConfigure.Controllers
{
    /// <summary>
    /// Controlador para gestionar las tareas programadas y crear tareas nuevas programadas para una fecha determinada
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class ScheduledJobController : ControllerBase
    {
        public ICronApiService _cronApiService;
        private IProgramingMethodService _programingMethodsService;
        public ScheduledJobController(ICronApiService cronApiService, IProgramingMethodService programingMethodsService)
        {
            _cronApiService = cronApiService;
            _programingMethodsService = programingMethodsService;
        }

        /// <summary>
        /// Obtiene un listado de tareas programadas 
        /// </summary>
        /// <param name="from">número desde el cual se va a traer las tareas del listado, por defecto 0 para empezar a traer desde el primer elemento de la lista de tareas</param>
        /// <param name="count">número máximo de tareas programadas a traer</param>
        /// <returns>listado de tareas programadas</returns> 
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult GetScheduledJobs(int from, int count)
        {
            return Ok(_cronApiService.GetScheduledJobs(from, count));
        }

        /// <summary>
        /// Añade una nueva tarea programada de única ejecución para sincornización de repositorios
        /// </summary>
        /// <param name="fecha_ejecucion">fecha en la que se ejecutará la tarea,el formato de fecha es: dd/MM/yyyy hh:mm ejemplo de formato de fecha: 07/05/2020 12:23</param>
        /// <param name="id_repository">identificador del repositorio,  este parametro se puede obtener con el método http://herc-as-front-desa.atica.um.es/carga/etl-config/Repository</param>
        /// <param name="fecha">fecha a partir de la cual se debe actualizar,el formato de fecha es: dd/MM/yyyy hh:mm ejemplo de formato de fecha: 07/05/2020 12:23</param>
        /// <param name="set">tipo del objeto, usado para filtrar por agrupaciones, este parametro se puede obtener de http://herc-as-front-desa.atica.um.es/carga/etl/ListSets/{identificador_del_repositorio}</param>
        /// <param name="codigo_objeto">codigo del objeto a sincronizar, es necesario pasar el parametro set si se quiere pasar este parámetro, este parametro se puede obtener en la respuesta identifier que da el método http://herc-as-front-desa.atica.um.es/carga/etl/ListIdentifiers/{identificador_del_repositorio}?metadataPrefix=rdf</param>
        /// <returns>identificador de la tarea creada</returns> 
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost]
        public IActionResult AddScheduledJob(string fecha_ejecucion, string id_repository, string fecha = null, string set = null, string codigo_objeto = null)
        {
            DateTime fechaInicio = DateTime.Now;
            DateTime? fechaDateTime = null;
            if (codigo_objeto != null && set == null)
            {
                return BadRequest("falta el tipo de objeto");
            }
            if (fecha_ejecucion != null)
            {
                try
                {
                    fechaInicio = DateTime.ParseExact(fecha_ejecucion, "dd/MM/yyyy hh:mm", null);
                }
                catch (Exception)
                {
                    return BadRequest("fecha de ejecución inválida");
                }
            }
            else
            {
                return BadRequest("La fecha de ejecución es obligatoria");
            }
            if (fecha != null)
            {
                try
                {
                    fechaDateTime = DateTime.ParseExact(fecha, "dd/MM/yyyy hh:mm", null);
                }
                catch (Exception)
                {
                    return BadRequest("fecha de sincronzación inválida");
                }
            }
            Guid idRep = Guid.Empty;
            try
            {
                idRep = new Guid(id_repository);
            }
            catch (Exception)
            {
                return BadRequest("identificador invalido");
            }
            return Ok(_programingMethodsService.ProgramPublishRepositoryJob(idRep, fechaInicio, fechaDateTime, set, codigo_objeto));
            
        }

        /// <summary>
        /// Añade a la cola una tarea que estaba prevista ejecutar en un futuro 
        /// </summary>
        /// <param name="id">identificador de la tarea programada, identificador que se obtiene al crear una tarea progranada o accesibke a través de http://herc-as-front-desa.atica.um.es/cron-config/ScheduledJob?from=0&amp;count=100</param>
        /// <returns></returns> 
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPut]
        public IActionResult EnqueuedScheduledJob(string id)
        {
            if(_cronApiService.ExistScheduledJob(id))
            {
                _cronApiService.EnqueueJob(id);
                return Ok();
            }
            else
            {
                return BadRequest("no existe la tarea programada");
            }
        }

        /// <summary>
        /// Elimina una tarea programada
        /// </summary>
        /// <param name="id">identificador de la tarea programada, identificador que se obtiene al crear una tarea progranada o accesibke a través de http://herc-as-front-desa.atica.um.es/cron-config/ScheduledJob?from=0&amp;count=100</param>
        /// <returns></returns> 
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpDelete]
        public IActionResult DeleteScheduledJob(string id)
        {
            if (_cronApiService.ExistScheduledJob(id))
            {
                _cronApiService.DeleteJob(id);
                return Ok();
            }
            else
            {
                return BadRequest("no existe la tarea programada");
            }
        }
    }
}
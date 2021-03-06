﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Xml;
using API_CARGA.Models.Entities;
using API_CARGA.Models.Services;
using API_CARGA.Models.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace API_CARGA.Controllers
{
    /// <summary>
    /// Contiene los procesos ETL (Extract, Transform and Load) necesarios para la carga de datos.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class etlController : Controller
    {
        private IRepositoriesConfigService _repositoriesConfigService;
        private IShapesConfigService _shapeConfigService;
        readonly ConfigSparql _configSparql;

        public etlController(IRepositoriesConfigService iRepositoriesConfigService, IShapesConfigService iShapeConfigService, ConfigSparql configSparql)
        {
            _repositoriesConfigService = iRepositoriesConfigService;
            _shapeConfigService = iShapeConfigService;
            _configSparql = configSparql;
        }

        /// <summary>
        /// Ejecuta el último paso del proceso de carga, por el que el RDF generado se almacena en el Triple Store. Permite cargar una fuente RDF arbitraria.
        /// Aquí se encuentra un RDF de Ejemplo: https://github.com/HerculesCRUE/GnossDeustoBackend/blob/master/API_CARGA/API_CARGA/Samples/rdfSample.xml
        /// </summary>
        /// <param name="rdfFile">Fichero RDF</param>
        /// <returns></returns>
        [HttpPost("data-publish")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult dataPublish(IFormFile rdfFile)
        {
            try
            {
                XmlDocument rdf = SparqlUtility.GetRDFFromFile(rdfFile);
                List<string> triples = SparqlUtility.GetTriplesFromRDF(rdf);    
                SparqlUtility.LoadTriples(triples, _configSparql.GetEndpoint(), _configSparql.GetQueryParam(), _configSparql.GetGraph(), _configSparql.GetGraphUnidata()); 
                return Ok();
            }
            catch (Exception ex)
            {
                return Problem(ex.ToString());
            }
        }

        /// <summary>
        /// Valida un RDF mediante el shape SHACL configurado
        /// Aquí se encuentra un RDF de Ejemplo: https://github.com/HerculesCRUE/GnossDeustoBackend/blob/master/API_CARGA/API_CARGA/Samples/rdfSample.xml
        /// </summary>
        /// <param name="rdfFile">Fichero RDF</param>
        /// <param name="repositoryIdentifier">Identificador del repositorio para seleccionar los Shapes (los repositorios disponibles están en /etl-config/repository)</param>
        /// <returns></returns>
        [HttpPost("data-validate")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Example", typeof(ShapeReport))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult dataValidate(IFormFile rdfFile, Guid repositoryIdentifier)
        {            
            try
            {
                string rdfFileContent = SparqlUtility.GetTextFromFile(rdfFile);
                return Ok(SparqlUtility.ValidateRDF(rdfFileContent, _shapeConfigService.GetShapesConfigs().FindAll(x => x.RepositoryID == repositoryIdentifier)));
            }
            catch (Exception ex)
            {
                return Problem(ex.ToString());
            }

        }

        /// <summary>
        /// **(Pendiente de implementar)Reconcilia entidades y descubre enlaces o equivalencias. Permite efectuar el descubrimiento en fuentes RDF arbitrarias.
        /// </summary>
        /// <param name="rdfFile">Fichero RDF</param>
        /// <returns></returns>
        [HttpPost("data-discover")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult dataDiscover(IFormFile rdfFile)
        {
            throw new Exception("TODO");
        }


        /// <summary>
        /// Este método hace de PROXY entre el API y el proveedor OAI-PMH.
        /// Recupera un registro de metadatos individual del repositorio en formato XML OAI-PMH.        
        /// </summary>
        /// <param name="identifier">Identificador de la entidad a recolectar (Los identificadores se obtienen con el metodo /etl/ListIdentifiers/{repositoryIdentifier}).</param>
        /// <param name="metadataPrefix">Prefijo del metadata que se desea recuperar (rdf). Los formatos de metadatos admitidos por un repositorio y para un elemento en particular se pueden recuperar mediante la solicitud /etl/ListMetadataFormats/{repositoryIdentifier}.</param>
        /// <param name="repositoryIdentifier">Identificador del repositorio OAI-PMH (los repositorios disponibles están en /etl-config/repository)</param>
        /// <returns>XML devuelto por el repositorio OAI-PMH</returns>
        [HttpGet("GetRecord/{repositoryIdentifier}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public FileResult GetRecord(Guid repositoryIdentifier, string identifier, string metadataPrefix)
        {
            RepositoryConfig repositoryConfig = _repositoriesConfigService.GetRepositoryConfigById(repositoryIdentifier);
            string uri = repositoryConfig.Url;
            uri += $"?verb=GetRecord&identifier={identifier}&metadataPrefix={metadataPrefix}";
            byte[] array = getByte(uri);
            return File(array, "application/xml");
        }

        /// <summary>
        /// Este método hace de PROXY entre el API y el proveedor OAI-PMH.
        /// Obtiene la información del repositorio OAI-PMH configurado en formato XML OAI-PMH.
        /// </summary>
        /// <param name="repositoryIdentifier">Identificador del repositorio OAI-PMH (los repositorios disponibles están en /etl-config/repository)</param>
        /// <returns>XML devuelto por el repositorio OAI-PMH</returns>
        [HttpGet("Identify/{repositoryIdentifier}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public FileResult Identify(Guid repositoryIdentifier)
        {
            RepositoryConfig repositoryConfig = _repositoriesConfigService.GetRepositoryConfigById(repositoryIdentifier);
            string uri = repositoryConfig.Url;
            uri += $"?verb=Identify";
            byte[] array = getByte(uri);
            return File(array, "application/xml");
        }

        /// <summary>
        /// Este método hace de PROXY entre el API y el proveedor OAI-PMH.
        /// Es una forma abreviada de ListRecords, que recupera solo encabezados en formato XML OAI-PMH en lugar de registros.        
        /// </summary>
        /// <param name="metadataPrefix">Especifica que los encabezados deben devolverse solo si el formato de metadatos que coincide con el metadataPrefix proporcionado está disponible o, según el soporte del repositorio para las eliminaciones, se ha eliminado. Los formatos de metadatos admitidos por un repositorio y para un elemento en particular se pueden recuperar mediante la solicitud ListMetadataFormats.</param>
        /// <param name="from">Fecha de inicio desde la que se desean recuperar las cabeceras de las entidades (Codificado con ISO8601 y expresado en UTC, YYYY-MM-DD o YYYY-MM-DDThh:mm:ssZ)</param>
        /// <param name="until">Fecha de fin hasta la que se desean recuperar las cabeceras de las entidades (Codificado con ISO8601 y expresado en UTC, YYYY-MM-DD o YYYY-MM-DDThh:mm:ssZ)</param>
        /// <param name="set">Argumento con un valor setSpec, que especifica los criterios establecidos para la recolección selectiva. Los formatos de sets admitidos por un repositorio y para un elemento en particular se pueden recuperar mediante la solicitud ListSets.</param>
        /// <param name="resumptionToken">Argumento exclusivo con un valor que es el token de control de flujo devuelto por una solicitud ListIdentifiers anterior que emitió una lista incompleta.</param>
        /// <param name="repositoryIdentifier">Identificador del repositorio OAI-PMH (los repositorios disponibles están en /etl-config/repository)</param>
        /// <returns>XML devuelto por el repositorio OAI-PMH</returns>
        [HttpGet("ListIdentifiers/{repositoryIdentifier}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public FileResult ListIdentifiers(Guid repositoryIdentifier, string metadataPrefix = null, DateTime? from = null, DateTime? until = null, string set = null, string resumptionToken = null)
        {
            RepositoryConfig repositoryConfig = _repositoriesConfigService.GetRepositoryConfigById(repositoryIdentifier);
            string uri = repositoryConfig.Url;
            uri += $"?verb=ListIdentifiers";
            if (metadataPrefix != null)
            {
                uri += $"&metadataPrefix={metadataPrefix}";
            }
            if (from.HasValue)
            {
                uri += $"&from={from.Value.ToString("u",CultureInfo.InvariantCulture)}";
            }
            if (until.HasValue)
            {
                uri += $"&until={until.Value.ToString("u",CultureInfo.InvariantCulture)}";
            }
            if (set != null)
            {
                uri += $"&set={set}";
            }
            if (resumptionToken != null)
            {
                uri += $"&resumptionToken={resumptionToken}";
            }
            byte[] array = getByte(uri);
            return File(array, "application/xml");
        }


        /// <summary>
        /// Este método hace de PROXY entre el API y el proveedor OAI-PMH.
        /// Recupera los formatos de metadatos disponibles del repositorio en formato XML OAI-PMH.        
        /// </summary>
        /// <param name="identifier">Argumento opcional que especifica el identificador único del elemento para el que se solicitan los formatos de metadatos disponibles. Si se omite este argumento, la respuesta incluye todos los formatos de metadatos admitidos por este repositorio. </param>
        /// <param name="repositoryIdentifier">Identificador del repositorio OAI-PMH (los repositorios disponibles están en /etl-config/repository)</param>
        /// <returns>XML devuelto por el repositorio OAI-PMH</returns>
        [HttpGet("ListMetadataFormats/{repositoryIdentifier}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public FileResult ListMetadataFormats(Guid repositoryIdentifier, string identifier = null)
        {
            RepositoryConfig repositoryConfig = _repositoriesConfigService.GetRepositoryConfigById(repositoryIdentifier);
            string uri = repositoryConfig.Url;
            uri += $"?verb=ListMetadataFormats";
            if (identifier != null)
            {
                uri += $"&identifier={identifier}";
            }
            byte[] array = getByte(uri);
            return File(array, "application/xml");
        }

        /// <summary>
        /// Este método hace de PROXY entre el API y el proveedor OAI-PMH.
        /// Recupera registros del repositorio en formato XML OAI-PMH.        
        /// </summary>
        /// <param name="metadataPrefix">Especifica que los encabezados deben devolverse solo si el formato de metadatos que coincide con el metadataPrefix proporcionado está disponible o, según el soporte del repositorio para las eliminaciones, se ha eliminado. Los formatos de metadatos admitidos por un repositorio y para un elemento en particular se pueden recuperar mediante la solicitud ListMetadataFormats.</param>
        /// <param name="from">Fecha de inicio desde la que se desean recuperar las cabeceras de las entidades (Codificado con ISO8601 y expresado en UTC, YYYY-MM-DD o YYYY-MM-DDThh:mm:ssZ)</param>
        /// <param name="until">Fecha de fin hasta la que se desean recuperar las cabeceras de las entidades (Codificado con ISO8601 y expresado en UTC, YYYY-MM-DD o YYYY-MM-DDThh:mm:ssZ)</param>
        /// <param name="set">Argumento con un valor setSpec, que especifica los criterios establecidos para la recolección selectiva. Los formatos de sets admitidos por un repositorio y para un elemento en particular se pueden recuperar mediante la solicitud ListSets.</param>
        /// <param name="resumptionToken">Argumento exclusivo con un valor que es el token de control de flujo devuelto por una solicitud ListRecords anterior que emitió una lista incompleta.</param>
        /// <param name="repositoryIdentifier">Identificador del repositorio OAI-PMH (los repositorios disponibles están en /etl-config/repository)</param>
        /// <returns>XML devuelto por el repositorio OAI-PMH</returns>
        [HttpGet("ListRecords/{repositoryIdentifier}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public FileResult ListRecords(Guid repositoryIdentifier, string metadataPrefix, DateTime? from = null, DateTime? until = null, string set = null, string resumptionToken = null)
        {
            RepositoryConfig repositoryConfig = _repositoriesConfigService.GetRepositoryConfigById(repositoryIdentifier);
            string uri = repositoryConfig.Url;
            uri += $"?verb=ListRecords";
            if (metadataPrefix != null)
            {
                uri += $"&metadataPrefix={metadataPrefix}";
            }
            if (from.HasValue)
            {
                uri += $"&from={from.Value.ToString("u", CultureInfo.InvariantCulture)}";
            }
            if (until.HasValue)
            {
                uri += $"&until={until.Value.ToString("u", CultureInfo.InvariantCulture)}";
            }
            if (set != null)
            {
                uri += $"&set={set}";
            }
            if (resumptionToken != null)
            {
                uri += $"&resumptionToken={resumptionToken}";
            }
            byte[] array = getByte(uri);
            return File(array, "application/xml");
        }

        /// <summary>
        /// Este método hace de PROXY entre el API y el proveedor OAI-PMH.
        /// Recuperar la estructura establecida de un repositorio en formato XML OAI-PMH, útil para la recolección selectiva.        
        /// </summary>
        /// <param name="resumptionToken">Argumento exclusivo con un valor que es el token de control de flujo devuelto por una solicitud ListSets anterior que emitió una lista incompleta.</param>
        /// <param name="repositoryIdentifier">Identificador del repositorio OAI-PMH (los repositorios disponibles están en /etl-config/repository)</param>
        /// <returns>XML devuelto por el repositorio OAI-PMH</returns>
        [HttpGet("ListSets/{repositoryIdentifier}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public FileResult ListSets(Guid repositoryIdentifier, string resumptionToken = null)
        {
            RepositoryConfig repositoryConfig = _repositoriesConfigService.GetRepositoryConfigById(repositoryIdentifier);
            string uri = repositoryConfig.Url;
            uri += $"?verb=ListSets";
            if (resumptionToken != null)
            {
                uri += $"&resumptionToken={resumptionToken}";
            }
            byte[] array = getByte(uri);
            return File(array, "application/xml");
        }

        private byte[] getByte(string URL)
        {
            HttpWebRequest wrGETURL = (HttpWebRequest)WebRequest.Create(URL);
            System.Net.HttpWebResponse webresponse = (HttpWebResponse)wrGETURL.GetResponse();
            string ct = webresponse.ContentType;
            Stream objStream = webresponse.GetResponseStream();
            BinaryReader breader = new BinaryReader(objStream);
            byte[] buffer = breader.ReadBytes((int)webresponse.ContentLength);
            return buffer;
        }
    }
}
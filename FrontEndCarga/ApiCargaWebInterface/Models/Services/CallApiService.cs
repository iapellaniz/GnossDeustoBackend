﻿using ApiCargaWebInterface.Extra.Exceptions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace ApiCargaWebInterface.Models.Services
{
    public class CallApiService : ICallService
    {
        readonly ConfigUrlService _serviceUrl;
        public CallApiService(ConfigUrlService serviceUrl)
        {
            _serviceUrl = serviceUrl;
        }

        public string CallDeleteApi(string urlMethod)
        {
            string result = "";
            HttpResponseMessage response = null;
            try
            {
                HttpClient client = new HttpClient();
                string url = _serviceUrl.GetUrl();
                response = client.DeleteAsync($"{url}{urlMethod}").Result;
                response.EnsureSuccessStatusCode();
                result = response.Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException)
            {
                if (!string.IsNullOrEmpty(response.Content.ReadAsStringAsync().Result))
                {
                    throw new HttpRequestException(response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    throw new HttpRequestException(response.ReasonPhrase);
                }
            }
            return result;
        }

        public string CallGetApi(string urlMethod)
        {
            string result = "";
            HttpResponseMessage response = null;
            try
            {
                HttpClient client = new HttpClient();
                string url = _serviceUrl.GetUrl();
                response = client.GetAsync($"{url}{urlMethod}").Result;
                response.EnsureSuccessStatusCode();
                result = response.Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException)
            {
                if (!string.IsNullOrEmpty(response.Content.ReadAsStringAsync().Result))
                {
                    throw new HttpRequestException(response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    throw new HttpRequestException(response.ReasonPhrase);
                }
            }
            return result;
        }

        public string CallPostApi(string urlMethod, object item, bool isFile = false)
        {
            HttpContent contentData = null;
            if (!isFile)
            {
                string stringData = JsonConvert.SerializeObject(item);
                contentData = new StringContent(stringData, System.Text.Encoding.UTF8, "application/json");
            }
            else
            {
                byte[] data;
                using (var br = new BinaryReader(((IFormFile)item).OpenReadStream()))
                {
                    data = br.ReadBytes((int)((IFormFile)item).OpenReadStream().Length);
                }
                ByteArrayContent bytes = new ByteArrayContent(data);
                contentData = new MultipartFormDataContent();
                ((MultipartFormDataContent)contentData).Add(bytes, "rdfFile", ((IFormFile)item).FileName);
            }
            string result = "";
            HttpResponseMessage response = null;
            try
            {
                HttpClient client = new HttpClient();
                string url = _serviceUrl.GetUrl();
                response = client.PostAsync($"{url}{urlMethod}", contentData).Result;
                response.EnsureSuccessStatusCode();
                result = response.Content.ReadAsStringAsync().Result;
                return result;
            }
            catch (HttpRequestException)
            {
                if (response.StatusCode.Equals(HttpStatusCode.BadRequest))
                {
                    throw new BadRequestException(response.Content.ReadAsStringAsync().Result);
                }
                else if (!string.IsNullOrEmpty(response.Content.ReadAsStringAsync().Result))
                {
                    throw new HttpRequestException(response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    throw new HttpRequestException(response.ReasonPhrase);
                }
            }
        }

        public string CallPutApi(string urlMethod, object item, bool isFile=false)
        {
            HttpContent contentData = null;
            if (!isFile)
            {
                string stringData = JsonConvert.SerializeObject(item);
                contentData = new StringContent(stringData, System.Text.Encoding.UTF8, "application/json");
            }
            else
            {
                if (item != null)
                { 
                    byte[] data;
                    using (var br = new BinaryReader(((IFormFile)item).OpenReadStream()))
                    {
                        data = br.ReadBytes((int)((IFormFile)item).OpenReadStream().Length);
                    }
                    ByteArrayContent bytes = new ByteArrayContent(data);
                    contentData = new MultipartFormDataContent();
                    ((MultipartFormDataContent)contentData).Add(bytes, "rdfFile", ((IFormFile)item).FileName);
                }
            }
            string result = "";
            HttpResponseMessage response = null;
            try
            {
                HttpClient client = new HttpClient();
                string url = _serviceUrl.GetUrl();
                response = client.PutAsync($"{url}{urlMethod}", contentData).Result;
                response.EnsureSuccessStatusCode();
                result = response.Content.ReadAsStringAsync().Result;
                return result;
            }
            catch (HttpRequestException)
            {
                if (response.StatusCode.Equals(HttpStatusCode.BadRequest))
                {
                    throw new BadRequestException(response.Content.ReadAsStringAsync().Result);
                }
                else if (!string.IsNullOrEmpty(response.Content.ReadAsStringAsync().Result))
                {
                    throw new HttpRequestException(response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    throw new HttpRequestException(response.ReasonPhrase);
                }
            }
        }
    }
}

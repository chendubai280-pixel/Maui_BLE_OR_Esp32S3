using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MauiApp2
{
    public class Rest_HTTP
    {
        private HttpClient _httpClient;

        public Rest_HTTP()
        {
            _httpClient = new HttpClient() { };//http依赖注入
        }
        /// <summary>
        /// GET请求
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public async Task<string> Get(string uri)
        {
            var response = await _httpClient.GetAsync(uri);//异步方法get string类型请求

            var data = await response.Content.ReadAsStringAsync();//读取响应内容

            return data;
        }
        /// <summary>
        /// post请求
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public async Task Post(string uri)
        {
            using StringContent jsonContent = new(JsonSerializer.Serialize(new
            {
                id = 2,
                name = "LED2",
                content = "OFF"
            }),
            Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await _httpClient.PostAsync(uri, jsonContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            //Console.WriteLine($"{jsonResponse}\n");
        }
        /// <summary>
        /// put请求(修改)
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public async Task Put(string uri)
        {
            using StringContent jsonContent = new(JsonSerializer.Serialize(new 
            { 
                id = 3,
                name = "LED3", 
                content = "ON" 
            }), Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await _httpClient.PutAsync(uri, jsonContent); 
            var jsonResponse = await response.Content.ReadAsStringAsync();
        }
        /// <summary>
        /// delete请求(删除)
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public async Task Delete(string uri) 
        {
            using HttpResponseMessage response = await _httpClient.DeleteAsync(uri); 
            var jsonResponse = await response.Content.ReadAsStringAsync(); 
        }
    }
}

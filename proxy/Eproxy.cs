﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

using ProxyNavigateur.Models;
using Rechercheur;

namespace proxy
{
    public class Eproxy : IEproxy
    {
        private ProxyServer proxyServer;
        private Dictionary<Guid, string> requestBodyHistory;
        private Rechercheur.Rechercheur r;
        private List<Sites> listeVerte;
        private List<Sites> listeRouge;
        private List<ListeDynamique> listeDynamique;

        public Eproxy()
        {
            proxyServer = new ProxyServer();
            proxyServer.TrustRootCertificate = true;
            requestBodyHistory = new Dictionary<Guid, string>();            
        }

        public void setRechercheur(Rechercheur.Rechercheur r)
        {
            this.r = r;
            listeVerte = r.getListeSites("Liste Verte");
            listeRouge = r.getListeSites("Liste Rouge");
            listeDynamique = r.GetListeDynamiques("Approprie");
        }

        public void StartProxy()
        {
            proxyServer.BeforeRequest += OnRequest;
            proxyServer.BeforeResponse += OnResponse;
            proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback += OnCertificateSelection;

            /*string test = "démarrage du proxy";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "/file/test.txt", true))
            {              
                file.WriteLine(test);
            }*/

            var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000, true)
            {
                // ExcludedHttpsHostNameRegex = new List<string>() { "google.com", "dropbox.com" }
            };

            proxyServer.AddEndPoint(explicitEndPoint);
            proxyServer.Start();

            var transparentEndPoint = new TransparentProxyEndPoint(IPAddress.Any, 8001, true)
            {
                GenericCertificateName = "google.com"
            };
            proxyServer.AddEndPoint(transparentEndPoint);

            foreach (var endPoint in proxyServer.ProxyEndPoints)
                /*Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} ",
                    endPoint.GetType().Name, endPoint.IpAddress, endPoint.Port);*/

            proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
            proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);
        }

        public void Stop()
        {
            proxyServer.BeforeRequest -= OnRequest;
            proxyServer.BeforeResponse -= OnResponse;
            proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback -= OnCertificateSelection;

            proxyServer.Stop();
        }

        public async Task OnRequest(object sender, SessionEventArgs e)
        {
            //Console.WriteLine(e.WebSession.Request.Url);

            var requestHeaders = e.WebSession.Request.RequestHeaders;
            bool validSite = false;

            var method = e.WebSession.Request.Method.ToUpper();
            if ((method == "POST" || method == "PUT" || method == "PATCH"))
            {
                //Get/Set request body bytes
                byte[] bodyBytes = await e.GetRequestBody();
                await e.SetRequestBody(bodyBytes);

                //Get/Set request body as string
                string bodyString = await e.GetRequestBodyAsString();
                await e.SetRequestBodyString(bodyString);

                requestBodyHistory[e.Id] = bodyString;
            }

            /*using (System.IO.StreamWriter file = new System.IO.StreamWriter(System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "/file/test.txt", true))
            {
                file.WriteLine(e.WebSession.Request.RequestUri.AbsoluteUri);
            }*/

            //Console.WriteLine("---------------------------->" + e.WebSession.Request.RequestUri.AbsoluteUri);

            //To cancel a request with a custom HTML content
            //Filter URL
            foreach (Sites site in listeVerte)
            {
                if (e.WebSession.Request.RequestUri.AbsoluteUri.Contains(site.nomSite))
                {
                    validSite = true;
                }
            }

            if (!validSite)
            {
                foreach (Sites site in listeRouge)
                {
                    if (e.WebSession.Request.RequestUri.AbsoluteUri.Contains(site.nomSite))
                    {
                        await e.Ok("<!DOCTYPE html>" +
                      "<html><body><h1>" +
                      "Website Blocked" +
                      "</h1>" +
                      "<p>N'Dèye VERMONT et Stelio ALIFIERAKIS ont bloqué cette page !!!</p>" +
                      "<p>Raison : Site dans la liste rouge</p>" +
                      "</body>" +
                      "</html>");
                    }
                }
            }

            if (!validSite)
            {
                foreach (ListeDynamique dyn in listeDynamique)
                {
                    if (e.WebSession.Request.RequestUri.AbsoluteUri.Contains(dyn.url))
                    {
                       await e.Ok("<!DOCTYPE html>" +
                      "<html><body><h1>" +
                      "Website Blocked" +
                      "</h1>" +
                      "<p>N'Dèye VERMONT a bloqué cette page !!!</p>" +
                      "<p>Raison : Site déjà bloqué</p>"+
                      "</body>" +
                      "</html>");
                    }
                }
            }
            /*if (e.WebSession.Request.RequestUri.AbsoluteUri.Contains("perdu"))
            {
                await e.Ok("<!DOCTYPE html>" +
                      "<html><body><h1>" +
                      "Website Blocked" +
                      "</h1>" +
                      "<p>Blocked by titanium web proxy.</p>" +
                      "</body>" +
                      "</html>");
            }*/

            //Redirect example
            /*if (e.WebSession.Request.RequestUri.AbsoluteUri.Contains("wikipedia.org"))
            {
                await e.Redirect("https://www.paypal.com");
            }*/
        }

        public async Task OnResponse(object sender, SessionEventArgs e)
        {
            if (requestBodyHistory.ContainsKey(e.Id))
            {
                //access request body by looking up the shared dictionary using requestId
                var requestBody = requestBodyHistory[e.Id];
            }

            //read response headers
            var responseHeaders = e.WebSession.Response.ResponseHeaders;

            // print out process id of current session
            //Console.WriteLine($"PID: {e.WebSession.ProcessId.Value}");

            //if (!e.ProxySession.Request.Host.Equals("medeczane.sgk.gov.tr")) return;
            if (e.WebSession.Request.Method == "GET" || e.WebSession.Request.Method == "POST")
            {
                if (e.WebSession.Response.ResponseStatusCode == "200")
                {
                    if (e.WebSession.Response.ContentType != null && e.WebSession.Response.ContentType.Trim().ToLower().Contains("text/html"))
                    {
                        byte[] bodyBytes = await e.GetResponseBody();
                        await e.SetResponseBody(bodyBytes);

                        string body = await e.GetResponseBodyAsString();

                        /*using (System.IO.StreamWriter file = new System.IO.StreamWriter(System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "/file/test.txt", true))
                        {
                            file.WriteLine(body);
                        }*/
                        //Console.WriteLine(r.valPhrase(body));
                        if (r.valPhrase(body)>20)
                        {
                            string theme = r.themePage(body);
                            //Console.WriteLine("Ca marche");
                            await e.SetResponseBodyString("<!DOCTYPE html>" +
                            "<html><body><h1>" +
                            "Website Blocked" +
                            "</h1>" +
                            "<p>N'Dèye VERMONT a bloqué cette page !!!</p>" +
                            "Raison : "+ theme +
                            "</body>" +
                            "</html>");
                        }
                        else
                        {
                            await e.SetResponseBodyString(body);
                        }
                        //Console.WriteLine("---------------------------->" + body);
                        
                    }
                }
            }
        }

        public Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            //set IsValid to true/false based on Certificate Errors
            if (e.SslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            {
                e.IsValid = true;
            }

            return Task.FromResult(0);
        }

        public Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
        {
            //set e.clientCertificate to override

            return Task.FromResult(0);
        }
    }
}

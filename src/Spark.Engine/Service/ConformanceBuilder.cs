﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Rest;

namespace Spark.Service
{
    public static class ConformanceBuilder
    {
        public static Conformance CreateServer(String server, String serverVersion, String publisher, String fhirVersion)
        {
            Conformance conformance = new Conformance();
            conformance.Name = server;
            conformance.Publisher = publisher;
            conformance.Version = serverVersion;
            conformance.FhirVersion = fhirVersion;
            conformance.AcceptUnknown = false;
            conformance.Date = Date.Today().Value;
            conformance.AddServer();
            return conformance;
            //AddRestComponent(true);
            //AddAllCoreResources(true, true, Conformance.ResourceVersionPolicy.VersionedUpdate);
            //AddAllSystemInteractions();
            //AddAllResourceInteractionsAllResources();
            //AddCoreSearchParamsAllResources();

            //return con;
        }

        public static Conformance.ConformanceRestComponent AddRestComponent(this Conformance conformance, Boolean isServer, String documentation = null, String mailbox = null)
        {
            var server = new Conformance.ConformanceRestComponent();
            server.Mode = (isServer) ? Conformance.RestfulConformanceMode.Server : Conformance.RestfulConformanceMode.Client;

            if (documentation != null)
            {
                server.Documentation = documentation;
            }

            if (mailbox != null)
            {
                var listmailbox = (List<String>)server.DocumentMailbox;
                listmailbox.Add(mailbox);
                server.DocumentMailbox = listmailbox;

            }
            conformance.Rest.Add(server);
            return server;
        }

        public static Conformance AddServer(this Conformance conformance)
        {
            conformance.AddRestComponent(isServer: true);
            return conformance;
        }

        public static Conformance.ConformanceRestComponent Server(this Conformance conformance)
        {
            var server = conformance.Rest.FirstOrDefault(r => r.Mode == Conformance.RestfulConformanceMode.Server);
            return (server == null) ? conformance.AddRestComponent(isServer: true) : server;
        }

        public static Conformance.ConformanceRestComponent Rest(this Conformance conformance)
        {
            return conformance.Rest.FirstOrDefault();
        }

        public static Conformance AddAllCoreResources(this Conformance conformance, Boolean readhistory, Boolean updatecreate, Conformance.ResourceVersionPolicy versioning)
        {
            foreach (var resource in ModelInfo.SupportedResources)
            {
                conformance.AddSingleResourceComponent(resource, readhistory, updatecreate, versioning);
            }
            return conformance;
        }

        public static Conformance AddMultipleResourceComponents(this Conformance conformance, List<String> resourcetypes, Boolean readhistory, Boolean updatecreate, Conformance.ResourceVersionPolicy versioning)
        {
            foreach (var type in resourcetypes)
            {
                AddSingleResourceComponent(conformance, type, readhistory, updatecreate, versioning);
            }
            return conformance;
        }

        public static Conformance AddSingleResourceComponent(this Conformance conformance, String resourcetype, Boolean readhistory, Boolean updatecreate, Conformance.ResourceVersionPolicy versioning, ResourceReference profile = null)
        {
            var resource = new Conformance.ConformanceRestResourceComponent();
            resource.Type = resourcetype;
            resource.Profile = profile;
            resource.ReadHistory = readhistory;
            resource.UpdateCreate = updatecreate;
            resource.Versioning = versioning;
            conformance.Server().Resource.Add(resource);
            return conformance;
        }

        public static Conformance AddSummaryForAllResources(this Conformance conformance)
        {
            foreach (var resource in conformance.Rest.FirstOrDefault().Resource.ToList())
            {
                var p = new Conformance.ConformanceRestResourceSearchParamComponent();
                p.Name = "_summary";
                p.Type = Conformance.SearchParamType.String;
                p.Documentation = "Summary for resource";
                resource.SearchParam.Add(p);
            }
            return conformance;
        }

        public static Conformance AddCoreSearchParamsAllResources(this Conformance conformance)
        {
            foreach (var r in conformance.Rest.FirstOrDefault().Resource.ToList())
            {
                conformance.Rest().Resource.Remove(r);
                conformance.Rest().Resource.Add(AddCoreSearchParamsResource(r));
            }
            return conformance;
        }


        public static Conformance.ConformanceRestResourceComponent AddCoreSearchParamsResource(Conformance.ConformanceRestResourceComponent resourcecomp)
        {
            var parameters = ModelInfo.SearchParameters.Where(sp => sp.Resource == resourcecomp.Type)
                            .Select(sp => new Conformance.ConformanceRestResourceSearchParamComponent
                            {
                                Name = sp.Name,
                                Type = sp.Type,
                                Documentation = sp.Description,
                                
                            });

            resourcecomp.SearchParam.AddRange(parameters);
            return resourcecomp;
        }

        public static Conformance AddAllInteractionsForAllResources(this Conformance conformance)
        {
            foreach (var r in conformance.Rest.FirstOrDefault().Resource.ToList())
            {
                conformance.Rest().Resource.Remove(r);
                conformance.Rest().Resource.Add(AddAllResourceInteractions(r));
            }
            return conformance;
        }

        public static Conformance.ConformanceRestResourceComponent AddAllResourceInteractions(Conformance.ConformanceRestResourceComponent resourcecomp)
        {
            foreach (Conformance.TypeRestfulInteraction type in Enum.GetValues(typeof(Conformance.TypeRestfulInteraction)))
            {
                var interaction = AddSingleResourceInteraction(resourcecomp, type);
                resourcecomp.Interaction.Add(interaction);
            }
            return resourcecomp;
        }

        public static Conformance.ResourceInteractionComponent AddSingleResourceInteraction(Conformance.ConformanceRestResourceComponent resourcecomp, Conformance.TypeRestfulInteraction type)
        {
            var interaction = new Conformance.ResourceInteractionComponent();
            interaction.Code = type;
            return interaction;

        }

        public static Conformance AddAllSystemInteractions(this Conformance conformance)
        {
            foreach (Conformance.SystemRestfulInteraction code in Enum.GetValues(typeof(Conformance.SystemRestfulInteraction)))
            {
                conformance.AddSystemInteraction(code);
            }
            return conformance;
        }

        public static void AddSystemInteraction(this Conformance conformance, Conformance.SystemRestfulInteraction code)
        {
            var interaction = new Conformance.SystemInteractionComponent();

            interaction.Code = code;

            conformance.Rest().Interaction.Add(interaction);
        }

        public static void AddOperation(this Conformance conformance, String name, ResourceReference definition)
        {
            var operation = new Conformance.ConformanceRestOperationComponent();

            operation.Name = name;
            operation.Definition = definition;

            conformance.Server().Operation.Add(operation);
        }

        public static String ConformanceToXML(this Conformance conformance)
        {
            return FhirSerializer.SerializeResourceToXml(conformance);
        }

    }

}

        // TODO: Code review Conformance replacement
        //public const string CONFORMANCE_ID = "self";
        //public static readonly string CONFORMANCE_COLLECTION_NAME = typeof(Conformance).GetCollectionName();
    
        //public static Conformance CreateTemp()
        //{
        //    return new Conformance();

        //}

        //public static Conformance Build()
        //{
        //    //var conformance = new Conformance();

            //Stream s = typeof(ConformanceBuilder).Assembly.GetManifestResourceStream("Spark.Engine.Service.Conformance.xml");
            //StreamReader sr = new StreamReader(s);
            //string conformanceXml = sr.ReadToEnd();
            
            //var conformance = (Conformance)FhirParser.ParseResourceFromXml(conformanceXml);

            //conformance.Software = new Conformance.ConformanceSoftwareComponent();
            //conformance.Software.Version = ReadVersionFromAssembly();
            //conformance.Software.Name = ReadProductNameFromAssembly();
            //conformance.FhirVersion = ModelInfo.Version;
            //conformance.Date = Date.Today().Value;
            //conformance.Meta = new Resource.ResourceMetaComponent();
            //conformance.Meta.VersionId = "0";

            //Conformance.ConformanceRestComponent serverComponent = conformance.Rest[0];

            // Replace anything that was there before...
            //serverComponent.Resource = new List<Conformance.ConformanceRestResourceComponent>();
                
            /*var allOperations = new List<Conformance.ConformanceRestResourceOperationComponent>()
            {   new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.Create },
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.Delete },
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.HistoryInstance },

                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.HistoryType },
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.Read },
                
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.SearchType },
                
                
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.Update },
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.Validate },            
                new Conformance.ConformanceRestResourceOperationComponent { Code = Conformance.RestfulOperationType.Vread },
            };

            foreach (var resourceName in ModelInfo.SupportedResources)
            {
                var supportedResource = new Conformance.ConformanceRestResourceComponent();
                supportedResource.Type = resourceName;
                supportedResource.ReadHistory = true;
                supportedResource.Operation = allOperations;

                // Add supported _includes for this resource
                supportedResource.SearchInclude = ModelInfo.SearchParameters
                    .Where(sp => sp.Resource == resourceName)
                    .Where(sp => sp.Type == Conformance.SearchParamType.Reference)
                    .Select(sp => sp.Name);

                supportedResource.SearchParam = new List<Conformance.ConformanceRestResourceSearchParamComponent>();


                var parameters = ModelInfo.SearchParameters.Where(sp => sp.Resource == resourceName)
                        .Select(sp => new Conformance.ConformanceRestResourceSearchParamComponent
                            {
                                Name = sp.Name,
                                Type = sp.Type,
                                Documentation = sp.Description,
                            });

                supportedResource.SearchParam.AddRange(parameters);
                
                serverComponent.Resource.Add(supportedResource);
            }
            */
            // This constant has become internal. Please undo. We need it. 

            // Update: new location: XHtml.XHTMLNS / XHtml
    //        // XNamespace ns = Hl7.Fhir.Support.Util.XHTMLNS;
    //        XNamespace ns = "http://www.w3.org/1999/xhtml";
           
    //        var summary = new XElement(ns + "div");

    //        foreach (string resourceName in ModelInfo.SupportedResources)
    //        {
    //            summary.Add(new XElement(ns + "p",
    //                String.Format("The server supports all operations on the {0} resource, including history",
    //                    resourceName)));
    //        }

    //        conformance.Text = new Narrative();
    //        conformance.Text.Div = summary.ToString();
    //        return conformance;
    //    }

    //    public static string ReadVersionFromAssembly()
    //    {
    //        var attribute = (System.Reflection.AssemblyFileVersionAttribute)typeof(ConformanceBuilder).Assembly
    //            .GetCustomAttributes(typeof(System.Reflection.AssemblyFileVersionAttribute), true)
    //            .Single();
    //        return attribute.Version;
    //    }

    //    public static string ReadProductNameFromAssembly()
    //    {
    //        var attribute = (System.Reflection.AssemblyProductAttribute)typeof(ConformanceBuilder).Assembly
    //            .GetCustomAttributes(typeof(System.Reflection.AssemblyProductAttribute), true)
    //            .Single();
    //        return attribute.Product;
    //    }
    //}
 
//}
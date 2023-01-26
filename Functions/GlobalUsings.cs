global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.IO;
global using System.Threading.Tasks;

global using Microsoft.Extensions.Logging;

global using Microsoft.Azure.WebJobs;
global using Microsoft.Azure.WebJobs.Extensions.DurableTask;

global using Azure;
global using Azure.Storage.Blobs;
global using Azure.Storage.Blobs.Models;
global using Azure.Storage.Blobs.Specialized;
global using Azure.Storage.Sas;

global using ImageIngest.Functions;
global using ImageIngest.Functions.Interfaces;
global using ImageIngest.Functions.Extensions;
global using ImageIngest.Functions.Enums;
global using ImageIngest.Functions.Model;

global using Newtonsoft.Json;



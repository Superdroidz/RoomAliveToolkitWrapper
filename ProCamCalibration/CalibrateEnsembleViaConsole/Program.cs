using RoomAliveToolkit;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel.Discovery;
using System.Collections.ObjectModel;

namespace CalibrateEnsembleViaConsole
{
    class Program
    {
        private static Collection<EndpointDiscoveryMetadata> DiscoverCameras()
        {
            var discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
            var findCriteria = new FindCriteria(typeof(KinectServer2));
            findCriteria.Duration = new TimeSpan(0, 0, 2);
            var services = discoveryClient.Find(findCriteria);
            discoveryClient.Close();
            Console.WriteLine("Found {0} Kinect servers.", services.Endpoints.Count);
            return services.Endpoints;
        }

        private static Collection<EndpointDiscoveryMetadata> DiscoverProjectors()
        {
            var discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
            var findCriteria = new FindCriteria(typeof(ProjectorServer));
            findCriteria.Duration = new TimeSpan(0, 0, 2);
            var services = discoveryClient.Find(findCriteria);
            discoveryClient.Close();
            Console.WriteLine("Found {0} projector servers.", services.Endpoints.Count);
            return services.Endpoints;
        }

        private static void DiscoverServers()
        {
            Console.WriteLine("Finding Kinect and projector servers...");
            var findKServers =
                Task<Collection<EndpointDiscoveryMetadata>>.Factory.StartNew(DiscoverCameras);
            var findPServers =
                Task<Collection<EndpointDiscoveryMetadata>>.Factory.StartNew(DiscoverProjectors);

            var kServers = findKServers.Result;
            var pServers = findPServers.Result;
            ensemble = new ProjectorCameraEnsemble(pServers.Count, kServers.Count);
            for (int i = 0; i < kServers.Count; ++i)
            {
                ensemble.cameras[i].hostNameOrAddress = kServers[i].Address.ToString();
                ensemble.cameras[i].name = i.ToString();
            }
            for (int i = 0; i < pServers.Count; ++i)
            {
                ensemble.projectors[i].hostNameOrAddress = pServers[i].Address.ToString();
                ensemble.projectors[i].name = i.ToString();
            }

            Console.WriteLine("Server search completion.");
        }

        private static void SaveXML()
        {
            try
            {
                ensemble.Save(XMLfilename);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not save configuration file to disk.\n" + ex);
                return;
            }
        }

        private static void LoadXML()
        {
            try
            {
                using (var fileStream = new FileStream(XMLfilename, FileMode.Open))
                {
                    var knownTypeList = new List<Type>();
                    knownTypeList.Add(typeof(Kinect2Calibration));
                    var serializer = new DataContractSerializer(typeof(ProjectorCameraEnsemble), knownTypeList);
                    ensemble = (ProjectorCameraEnsemble)serializer.ReadObject(fileStream);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not open XML file.\n" + e);
            }
        }

        private static void SaveObj()
        {
            Console.WriteLine("Creating object file...");
            try
            {
                ensemble.SaveToOBJ(directory, directoryName);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not save object file to disk.\n" + e);
                return;
            }
            Console.WriteLine("Object file created.");
        }

        private static void Acquire()
        {
            Console.WriteLine("Acquiring...");
            try
            {
                ensemble.CaptureGrayCodes(directory);
            }
            catch (Exception e)
            {
                Console.WriteLine("Acquire failed.\n" + e);
            }
            Console.WriteLine("Acquire complete.");
        }

        private static void Solve()
        {
            Console.WriteLine("Solving...");

            ensemble.DecodeGrayCodeImages(directory);
            try
            {
                ensemble.CalibrateProjectorGroups(directory);
                ensemble.OptimizePose();
            }
            catch (Exception e)
            {
                Console.WriteLine("Solve failed.\n" + e);
            }
            Console.WriteLine("Solved!");
        }

        static string op;
        static string directory;
        static string directoryName;
        static string XMLfilename;
        static ProjectorCameraEnsemble ensemble;

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Incorrect number of arguments supplied!");
            }
            op = args[0];
            directory = args[1];
            directoryName = Path.GetFileName(directory);
            XMLfilename = Path.Combine(directory, directoryName + ".xml");

            switch (op)
            {
                case "create":
                    DiscoverServers();
                    SaveXML();
                    break;
                case "acquire":
                    LoadXML();
                    Acquire();
                    SaveXML();
                    break;
                case "solve":
                    LoadXML();
                    Solve();
                    SaveXML();
                    SaveObj();
                    break;
                default:
                    Console.WriteLine("Unrecognised command {0}", op);
                    break;
            }
        }
    }
}

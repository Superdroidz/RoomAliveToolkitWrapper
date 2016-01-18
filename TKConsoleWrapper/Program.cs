using RoomAliveToolkit;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel.Discovery;
using System.Collections.ObjectModel;
using System.Net;

namespace ConsoleCalibration
{
    class ConsoleProgram
    {
        static string op;
        static string directory;
        static string fileName;
        static string XMLfilename;
        static ProjectorCameraEnsemble ensemble;

        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Incorrect number of arguments supplied!");
            }
            op = args[0];
            directory = args[1];
            fileName = Path.GetFileNameWithoutExtension(args[2]);
            XMLfilename = Path.Combine(directory, fileName + ".xml");

            switch (op)
            {
                case "create":
                    DiscoverServers();
                    SaveXML();
                    break;
                case "calibrate":
                    LoadXML();
                    Acquire();
                    Solve();
                    SaveXML();
                    SaveObj();
                    break;
                case "show":
                    LoadXML();
                    ShowDisplayIndices();
                    break;
                case "hide":
                    LoadXML();
                    HideDisplayIndices();
                    break;
                default:
                    Console.WriteLine("Unrecognised command {0}", op);
                    break;
            }
        }

        static Collection<EndpointDiscoveryMetadata> DiscoverCameras()
        {
            var discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
            var findCriteria = new FindCriteria(typeof(KinectServer2));
            findCriteria.Duration = new TimeSpan(0, 0, 2);
            var services = discoveryClient.Find(findCriteria);
            discoveryClient.Close();
            Console.WriteLine("Found {0} Kinect servers.", services.Endpoints.Count);
            return services.Endpoints;
        }

        static Collection<EndpointDiscoveryMetadata> DiscoverProjectors()
        {
            var discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
            var findCriteria = new FindCriteria(typeof(ProjectorServer));
            findCriteria.Duration = new TimeSpan(0, 0, 2);
            var services = discoveryClient.Find(findCriteria);
            discoveryClient.Close();
            Console.WriteLine("Found {0} projector servers.", services.Endpoints.Count);
            return services.Endpoints;
        }

        static void DiscoverServers()
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
                ensemble.cameras[i].name = i.ToString();
                ensemble.cameras[i].hostNameOrAddress = kServers[i].Address.Uri.DnsSafeHost.ToString();
            }
            for (int i = 0; i < pServers.Count; ++i)
            {
                ensemble.projectors[i].name = i.ToString();
                ensemble.projectors[i].hostNameOrAddress = pServers[i].Address.Uri.DnsSafeHost.ToString();
                ensemble.projectors[i].displayIndex = 1;//Projectors start from 1.
            }
        }

        static void SaveXML()
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

        static void LoadXML()
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

        static void SaveObj()
        {
            Console.WriteLine("Creating object file...");
            try
            {
                ensemble.SaveToOBJ(directory, directory + "/" + fileName + ".obj");
                Console.WriteLine("Object file created.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not save object file to disk.\n" + e);
                return;
            }
        }

        static void Acquire()
        {
            Console.WriteLine("Acquiring...");
            try
            {
                ensemble.CaptureGrayCodes(directory);
                Console.WriteLine("Acquire complete.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Acquire failed.\n" + e);
            }
        }

        static void Solve()
        {
            Console.WriteLine("Solving...");

            ensemble.DecodeGrayCodeImages(directory);
            try
            {
                ensemble.CalibrateProjectorGroups(directory);
                ensemble.OptimizePose();
                Console.WriteLine("Solved!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Solve failed.\n" + e);
            }
        }

        static void ShowDisplayIndices()
        {
            try
            {
                foreach (var projector in ensemble.projectors)
                {
                    int screenCount = projector.Client.ScreenCount();
                    for (int i = 0; i < screenCount; i++)
                    {
                        projector.Client.OpenDisplay(i);
                        projector.Client.DisplayName(i, projector.hostNameOrAddress + ":" + i);
                    }
                }
                Console.WriteLine("Showing display indices.");
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to show display indices.");
            }
        }

        static void HideDisplayIndices()
        {
            try
            {
                foreach (var projector in ensemble.projectors)
                {
                    int screenCount = projector.Client.ScreenCount();
                    for (int i = 0; i < screenCount; i++)
                        projector.Client.CloseDisplay(i);
                }
                Console.WriteLine("Hid display indices.");
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to hide display indices.");
            }
        }
    }
}

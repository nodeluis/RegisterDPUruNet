using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DPUruNet;

namespace RegistroTickeo
{
    internal class Program
    {
        //finger serve

        static List<Fmd> tempFmds = new List<Fmd>();

        static Reader fReader;
        private static bool reset = false;
        private static Thread threadFReader;
        static WatsonWebsocket.WatsonWsServer server;
        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando el servidor");
            MyWebSocket socket = new MyWebSocket(5555);
            server = socket.StartServer();
            string message = "";
            while (true)
            {
                StartUpFingerReader();
                Console.WriteLine("Presione cualquier tecla para cerrar");
                message = Console.ReadLine();
                break;
            }
        }

        private static void StartUpFingerReader()
        {
            KillFingerReader();
            ReaderCollection freaders = ReaderCollection.GetReaders();

            foreach (Reader Reader in freaders)
            {
                fReader = Reader;
                break;
            }

            if (fReader == null)
            {
                Console.WriteLine("No se encontraron Lectores!");
                //Application.Current.Shutdown(); 
                return;
            }
            reset = false;

            threadFReader = new Thread(FRreaderMainFunction);
            threadFReader.IsBackground = true;
            threadFReader.Start();
        }

        private static void KillFingerReader()
        {


            if (threadFReader != null)
            {
                reset = true;
                if (fReader != null) fReader.Dispose();
            }

        }

        private static void FRreaderMainFunction()
        {

            //cont = 0;
            Constants.ResultCode result = Constants.ResultCode.DP_DEVICE_FAILURE;

            result = fReader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);

            if (result != Constants.ResultCode.DP_SUCCESS)
            {
                Console.WriteLine("Error abriendo el lector!:  " + result);
                /*if (_sender.CurrentReader != null)
                {
                    _sender.CurrentReader.Dispose();
                    _sender.CurrentReader = null;
                }*/
                //Application.Current.Shutdown();
            }

            //SendMessage("Ponga su dedo en el Lector para continuar.");


            while (!reset)
            {
                Fid fid = null;
                if (!CaptureFinger(ref fid))
                {
                    break;
                }

                if (fid == null)
                {
                    continue;
                }

                DataResult<Fmd> resultConversion = FeatureExtraction.CreateFmdFromFid(fid, Constants.Formats.Fmd.ANSI);
                Fmd currentFmd = resultConversion.Data;

                //string serializado = Fmd.SerializeXml(resultConversion.Data);
                //Fmd fff = Fmd.DeserializeXml(serializado);

                if (resultConversion.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    break;
                }
                //cont++;
                /////////////////////////////////////////////////AQUI VAMOS CON LA LOGICA!!!
                //AddMessage("OK");
                Console.WriteLine("Dedo Capturado... Lectura:" + (tempFmds.Count + 1));
                MyWebSocket.SendMessage(server,(tempFmds.Count + 1).ToString());
                tempFmds.Add(currentFmd);

                DataResult<Fmd> resultEnrollment = DPUruNet.Enrollment.CreateEnrollmentFmd(Constants.Formats.Fmd.ANSI, tempFmds);
                if (resultEnrollment.ResultCode == Constants.ResultCode.DP_SUCCESS)
                {
                    Console.WriteLine("FMD CREADO!!!");
                    string resp = Fmd.SerializeXml(resultEnrollment.Data);
                    Console.WriteLine(resp);
                    MyWebSocket.SendMessage(server, resp);
                    tempFmds.Clear();
                    break;
                }
                if (tempFmds.Count > 3)
                {
                    Console.WriteLine("Error creando el FMD...");
                    MyWebSocket.SendMessage(server, "error");
                    tempFmds.Clear();
                    break;

                }
            }

            if (fReader != null) { fReader.Dispose(); }

            StartUpFingerReader();
        }

        public static bool CaptureFinger(ref Fid fid)
        {
            try
            {
                Constants.ResultCode result = fReader.GetStatus();

                if ((result != Constants.ResultCode.DP_SUCCESS))
                {
                    Console.WriteLine("Get Status Error: " + result);
                    if (fReader != null)
                    {
                        fReader.Dispose();
                        fReader = null;
                    }
                    return false;
                }

                if ((fReader.Status.Status == Constants.ReaderStatuses.DP_STATUS_BUSY))
                {
                    Thread.Sleep(50);
                    return true;
                }
                else if ((fReader.Status.Status == Constants.ReaderStatuses.DP_STATUS_NEED_CALIBRATION))
                {
                    fReader.Calibrate();
                }
                else if ((fReader.Status.Status != Constants.ReaderStatuses.DP_STATUS_READY))
                {
                    Console.WriteLine("Get Status:  " + fReader.Status.Status);
                    if (fReader != null)
                    {
                        fReader.Dispose();
                        fReader = null;
                    }
                    return false;
                }

                CaptureResult captureResult = fReader.Capture(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, 500, fReader.Capabilities.Resolutions[0]);

                if (captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    Console.WriteLine("Error:  " + captureResult.ResultCode);
                    if (fReader != null)
                    {
                        fReader.Dispose();
                        fReader = null;
                    }
                    return false;
                }

                if (captureResult.Quality == Constants.CaptureQuality.DP_QUALITY_CANCELED)
                {
                    return false;
                }

                if ((captureResult.Quality == Constants.CaptureQuality.DP_QUALITY_NO_FINGER || captureResult.Quality == Constants.CaptureQuality.DP_QUALITY_TIMED_OUT))
                {
                    return true;
                }

                if ((captureResult.Quality == Constants.CaptureQuality.DP_QUALITY_FAKE_FINGER))
                {
                    Console.WriteLine("Quality Error:  " + captureResult.Quality);
                    return true;
                }

                fid = captureResult.Data;

                return true;
            }
            catch
            {
                Console.WriteLine("An error has occurred.");
                if (fReader != null)
                {
                    fReader.Dispose();
                    fReader = null;
                }
                return false;
            }
        }
    }
}

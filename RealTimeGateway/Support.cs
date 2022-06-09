using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeGateway
{
    public static class Support
    {
        public static void Print(object? value)
        {
            System.Diagnostics.Debug.WriteLine(value);
        }
        public static int RemoveComma(string value)
        {
            if (value.Contains(","))
            {
                value = value.Replace(",", string.Empty);
            }
            if(value.Contains(":"))
            {
                value = value.Replace(":", string.Empty);
            }
            return int.Parse(value);
        }
        public static int ReturnTotalDigits(int value)
        {
            int count = 0;
            while(value > 0)
            {
                value /= 10;
                count++;
            }
            return count;
        }
        public static string ReturnImageID(string image)
        {
            int index = image.IndexOf("Image UID: ");
            image = image.Substring(index + 11);
            index = image.IndexOf("Filename:");
            image = image.Substring(0, index);
            return image;
        }
        public static string GetStudyUID(string line)
        {
            // input requires GetStudyAndIMageUID output first
            int index = line.IndexOf(" ");
            string studyID = line.Substring(0, index + 2);
            return studyID;
        }
        public static string GetStudyUIDWhenFinished(string line)
        {
            // input requires GetStudyAndImageUID output first
            int index = line.IndexOf(" ");
            string studyID = line.Substring(0, index);
            return studyID;
        }
        public static string GetStudyUIDWhenClosed(string line)
        {
            int index = line.IndexOf("study UID: ");
            string uid = line.Substring(index + 11);
            return uid;
        }
        public static string GetStudyAndImageUID(string line)
        {
            int index = line.IndexOf("UID:");
            string uid = line.Substring(index + 5);
            return uid;
        }
        public static string GetStorageNamespace(string line)
        {
            int index = line.IndexOf("storage namespace ");
            string storage = line.Substring(index + 18);
            index = storage.IndexOf(" ");
            storage = storage.Substring(0, index);
            Print(storage);
            return storage;
        }

        public static string FindTotalImageNumber(string line)
        {
            int index = line.IndexOf("closing.");
            string totalImages = line.Substring(index + 8);
            index = totalImages.IndexOf("NON");
            totalImages = totalImages.Substring(0, index);

            return totalImages;
        }

        public static string ReturnFailedImage(string line)
        {
            int index = line.IndexOf("image_uid=");
            string returnedImage = line.Substring(index + 10);
            index = returnedImage.IndexOf("]");
            returnedImage = returnedImage.Substring(0, index);

            return returnedImage;
        }
        public static string ReturnFailedStudy(string line)
        {
            int index = line.IndexOf("study_uid=");
            string study = line.Substring(index + 10);
            index = study.IndexOf("&");
            study = study.Substring(0, index);

            return study;
        }

        public static bool CheckInternetConnection()
        {
            try
            {
                Ping myPing = new Ping();
                String host = "google.com";
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                if(reply.Status == IPStatus.Success)
                {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

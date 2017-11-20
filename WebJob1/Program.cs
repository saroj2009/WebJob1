using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Data.SqlClient;
using System.Configuration;
using System.Net.Mail;
using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Globalization;

namespace WebJob1
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }
            string s = DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            string StrGetDailyStatus = "";
            StrGetDailyStatus = GetDailyStatus(s);
            
            WriteToBlob(StrGetDailyStatus);
            var host = new JobHost();
            // The following code ensures that the WebJob will be running continuously
           // host.RunAndBlock();
        }
        public static SqlConnection con;
        public static void Connection()
        {
            string constr = "Data Source=tcp:sarojwebappdb.database.windows.net,1433;Initial Catalog=sarojwebappdb;User ID=sarojwebappdb;Password=Saroj@12345678;Encrypt=True;TrustServerCertificate=False";
            con = new SqlConnection(constr);
        }
       
        public static string GetDailyStatus(string date)
        {
            Connection();

            string SalesDetails = "";
            con.Open();
            try
            {
                string StrCommand = @"select Location, count(Status) Status  
                            FROM tbl2DailyStatus  
                            WHERE Date= '" + date + "' group by Location";

                using (SqlCommand command = new SqlCommand(StrCommand, con))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SalesDetails = SalesDetails + reader.GetString(0) + "\t" + reader.GetInt32(1) + "</br>";
                    }
                }


                con.Close();
            }
            catch (Exception e)
            {
                con.Close();
                Console.WriteLine("Could not retrieve Information " + e.Message);

                throw;
            }

            Console.WriteLine("Information Retrieved Successfully");

            return SalesDetails;
        }
        public void SendEmail(string msg)
        {
            string emailFrom = "";
            string emailTo = "";
            string subject = "";
            string smtpAddress = "";
            int portNumber = 0;
            string password = "";
            bool enableSSL = true;
            try
            {
                MailMessage mail = new MailMessage();

                mail.From = new MailAddress(emailFrom);
                mail.To.Add(emailTo);
                mail.Subject = subject;
                mail.Body = msg;
                mail.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient(smtpAddress, portNumber))
                {
                    smtp.Credentials = new NetworkCredential(emailFrom, password);
                    smtp.EnableSsl = enableSSL;
                    smtp.Send(mail);
                    Console.WriteLine("Email sent to " + emailTo + " at " + DateTime.Now);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Email failed " + e.Message);
                throw;
            }
        }
        public static void WriteToBlob(string salesreport)
        {
            try
            {
                string acs = "DefaultEndpointsProtocol=https;AccountName=sarojwebappstorage;AccountKey=bcXBWqEdljs7PbmVM83w+AtYqYazQIhp2O+9gikYWwlC2a4fNTHVnvgc83ETZpLquQTYGTl+4CrupCK4zWnXDg==";

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(acs);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("salesinfo");
                container.CreateIfNotExists();
                CloudBlockBlob blob = container.GetBlockBlobReference("DailyReport" + DateTime.Now + ".txt");
                blob.UploadText(salesreport);

                Console.WriteLine("File saved successfully"); ;

            }
            catch (Exception e)
            {
                Console.WriteLine("Saving to blob failed " + e.Message);

                throw;
            }
        }
    }
}

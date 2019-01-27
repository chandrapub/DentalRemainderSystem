using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Net;
using System.Net.Mime;
using System.Net.Mail;
using System.IO;
namespace DentalReminder
{
    class Program
    {
        static void Main(string[] args)
        {
            SqlConnection conn = new SqlConnection(@"data source = (local)\sqlexpress; integrated security = true; database = RemainderSystem; MultipleActiveResultSets = True");
            CheckReminders(conn);
            Console.ReadKey();
        }
        static void CheckReminders(SqlConnection connection)
        {
            using (connection)
            {
                connection.Open();

                //get a list of userappointments that need their 3day reminders sent 
                string s0s2 = "select * from appointment where state = 0 and DATEADD(day,3, CONVERT(datetime, CURRENT_TIMESTAMP)) >= AppointmentTime";
                SqlCommand command = new SqlCommand(s0s2, connection);
                SqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read()) //run trough the list of userappointments from above 
                    {
                        int id = reader.GetInt32(reader.GetOrdinal("ID"));
                        int affectedRows = UpdateState(connection, id, 0, 1); //update userappointments one at a time  

                        if (affectedRows > 0) // send email if the current userappointment was successfully updated to status 1
                        {
                            Program p = new Program();
                            p.SendEmail(reader["email"].ToString(),
                                "Dentist reminder",
                                "Remember the appointment you have in 3 days.");
                            UpdateState(connection, id, 1, 2); //note to database that we have sent the reminder email
                        }
                    }
                }
                else
                {
                    Console.WriteLine("3 day reminder handling concluded.");
                }

                string s0s3 = "select * from appointment where state = 2 and DATEADD(day,1, CONVERT(datetime, CURRENT_TIMESTAMP)) >= AppointmentTime";
                SqlCommand command2 = new SqlCommand(s0s3, connection);
                SqlDataReader reader2 = command2.ExecuteReader();

                if (reader2.HasRows)
                {
                    while (reader2.Read())
                    {
                        int id = reader2.GetInt32(reader2.GetOrdinal("ID"));
                        int affectedRows = UpdateState(connection, id, 2, 3);

                        if (affectedRows > 0)
                        {
                            Program p2 = new Program();
                            p2.SendSms(reader2["Mobile"].ToString(),
                                "Remember your appointment is tomorrow!",
                                "Dentist");
                            UpdateState(connection, id, 3, 4);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("1 day reminder handling concluded.");
                }
                reader.Close();
            }
        }

        static int UpdateState(SqlConnection connection, int id, int stateOld, int stateNew)
        {
            Console.Write("Updating State for id:" + id + " " + stateOld + "->" + stateNew);
            using (var updCmds0s1 = new SqlCommand("update appointment set state = " + stateNew + " where state = " + stateOld + " and id =" + id, connection))
            {
                int affectedRows = updCmds0s1.ExecuteNonQuery();
                Console.WriteLine(" (Affected rows:" + affectedRows + ")");

                return affectedRows;
            }
        }

        static int UpdateStateSms(SqlConnection connection, int id, int stateOld, int stateNew)
        {
            Console.Write("Updating State for id:" + id + " " + stateOld + "->" + stateNew);
            using (var updCmds0s2 = new SqlCommand("update appointment set state = " + stateNew + " where state = " + stateOld + " and id =" + id, connection))
            {
                int affectedRows = updCmds0s2.ExecuteNonQuery();
                Console.WriteLine(" (Affected rows:" + affectedRows + ")");

                return affectedRows;
            }
        }

        public void SendEmail(string emailTo, string subject, string body)
        {
            //Gmail credentials

            string from = "seemaranihowlader@gmail.com";
            string pass = "01714809136";
            MailMessage mail = new MailMessage(from, emailTo);
            mail.Subject = subject;
            mail.Body = body;
            SmtpClient client = new SmtpClient();
            //client.Port = 465;
            client.Port = 587;
            client.EnableSsl = true;
            client.Timeout = 10000;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential(from, pass);
            client.Host = "smtp.gmail.com";
            client.Send(mail);
        }

        public string SendSms(string msisdn, string message, string from)
        {
            //string username = "cphsr178";
            string username = "chandrapub";
            string apikey = "fe173d65-039f-****-****-3827fb7fa1e6";
            string basicauth = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + apikey));

            string url = "https://api.cpsms.dk/v2/simplesend/"
                + msisdn + "/" + System.Uri.EscapeDataString(message) + "/" + System.Uri.EscapeDataString(from);

            using (WebClient client = new WebClient())
            {
                client.Headers["Authorization"] = "Basic " + basicauth;
                return client.DownloadString(url);
            }
        }
    }
}

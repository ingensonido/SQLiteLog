using System;
using System.Text;
using System.Data.SQLite;
using System.Collections.Specialized;
using System.Net;
using System.Configuration;
using System.Collections.Generic;

namespace SQLiteLog
{
    class Program
    {

        static void Main(string[] args)
        {
            int avisos = 0;
            int tandasLocales = 0;
            int nRelleno = 0;
            int cuentaTandas = 0;
            double mayorRelleno = 0;
            double totalRelleno = 0;
            int id;
            String nombre;
            String hora;
            bool relleno = false;
            TimeSpan horaRelleno = new TimeSpan();
            TimeSpan horaStop = new TimeSpan();
            TimeSpan horaMayorRelleno = new TimeSpan();
            TimeSpan primerPlay = new TimeSpan();
            TimeSpan ultimoPlay = new TimeSpan();
            int h;
            int m;
            int s;
            DateTime ayer;
            SQLiteConnection conexion_sqlite;
            SQLiteCommand cmd_sqlite;
            SQLiteDataReader datareader_sqlite;
            List<int> lista = new List<int>();

            ayer = DateTime.Now.AddDays(-1);
            string archivo = ayer.ToString("dd-MM-yyyy") + ".sqlite";
            string anio = ayer.Year.ToString();
            string mes = ayer.Month.ToString();
            string ruta = ConfigurationManager.AppSettings["ruta"] + anio + "\\" + mes + "\\";

            //crearconexion a bd
            conexion_sqlite = new SQLiteConnection("Data Source =" + ruta + archivo + "; Version=3;");


            //Abrir conexion
            conexion_sqlite.Open();

            //Creando el comando SQL
            cmd_sqlite = conexion_sqlite.CreateCommand();

            //El Objeto SQLite

            cmd_sqlite.CommandText = "SELECT repro_Id, repro_Nombre, repro_Hora FROM log_reproduccion order by repro_Id";
            datareader_sqlite = cmd_sqlite.ExecuteReader();



            while (datareader_sqlite.Read())
            {
                id = datareader_sqlite.GetInt16(0);
                nombre = datareader_sqlite.GetString(1);
                hora = datareader_sqlite.GetString(2);

                //Console.WriteLine(id + " --- " + nombre + " --- " + hora);
                // Cuenta cantidad de tandas emitidas, por la cantidad de PLAY LOCAL

                if (nombre == "PLAY LOCAL")
                {
                    tandasLocales++;
                    lista.Add(datareader_sqlite.GetInt16(0));
                }
                //
                if (nombre.Contains("Pista"))
                {
                    relleno = true;
                    nRelleno++;
                    h = int.Parse(datareader_sqlite.GetString(2).Substring(0, 2));
                    m = int.Parse(datareader_sqlite.GetString(2).Substring(3, 2));
                    s = int.Parse(datareader_sqlite.GetString(2).Substring(6, 2));
                    horaRelleno = new TimeSpan(h, m, s);
                    //Console.WriteLine("RELLENO : " + horaRelleno);
                    //Console.WriteLine(datareader_sqlite.GetString(2));
                }

                if (nombre == "STOP LOCAL" && relleno)
                {
                    h = int.Parse(datareader_sqlite.GetString(2).Substring(0, 2));
                    m = int.Parse(datareader_sqlite.GetString(2).Substring(3, 2));
                    s = int.Parse(datareader_sqlite.GetString(2).Substring(6, 2));
                    horaStop = new TimeSpan(h, m, s);
                    //Console.WriteLine("Tiempo Relleno : " + horaStop.Subtract(horaRelleno).TotalSeconds);
                    totalRelleno = totalRelleno + horaStop.Subtract(horaRelleno).TotalSeconds;
                    //cuenta tandas que rellenaron
                    if (horaStop.Subtract(horaRelleno).TotalSeconds > 0)
                    {
                        cuentaTandas++;
                    }

                    //Console.WriteLine(datareader_sqlite.GetString(2));
                    relleno = false;
                }
                if (horaStop.Subtract(horaRelleno).TotalSeconds > mayorRelleno)
                {
                    mayorRelleno = horaStop.Subtract(horaRelleno).TotalSeconds;
                    horaMayorRelleno = horaStop;
                }
                //cantidad de avisos emitidos
                if (!nombre.Equals("PLAY LOCAL") & !nombre.Equals("STOP LOCAL") & !nombre.StartsWith("Pista"))
                {
                    avisos++;
                }

                if (id == lista[0])
                {
                    h = int.Parse(datareader_sqlite.GetString(2).Substring(0, 2));
                    m = int.Parse(datareader_sqlite.GetString(2).Substring(3, 2));
                    s = int.Parse(datareader_sqlite.GetString(2).Substring(6, 2));
                    primerPlay = new TimeSpan(h, m, s);
                }


                if (id == lista[tandasLocales - 1])
                {
                    h = int.Parse(datareader_sqlite.GetString(2).Substring(0, 2));
                    m = int.Parse(datareader_sqlite.GetString(2).Substring(3, 2));
                    s = int.Parse(datareader_sqlite.GetString(2).Substring(6, 2));
                    ultimoPlay = new TimeSpan(h, m, s);
                }


            }


            //En Consola

            Console.WriteLine("FECHA... : " + ayer.ToString("dd-MM-yyyy"));
            Console.WriteLine("Primer Play.... : " + primerPlay.ToString());
            Console.WriteLine("Ultimo Play.... : " + ultimoPlay.ToString());
            Console.WriteLine("Total de avisos.... : " + avisos.ToString());
            Console.WriteLine("Total de tandas locales.... : " + tandasLocales.ToString());
            Console.WriteLine("Total de tandas con relleno : " + cuentaTandas.ToString());
            Console.WriteLine("Total de relleno : " + totalRelleno.ToString() + " seg.");
            Console.WriteLine("Promedio de relleno : " + (totalRelleno / cuentaTandas).ToString("N2") + " seg.");
            Console.WriteLine("Tanda con mayor relleno : " + horaMayorRelleno.ToString() + " Duracion : " + mayorRelleno + " seg.");

            //Cerrando Conexion

            conexion_sqlite.Close();

            //Ejecuta lo anterior

            String url = ConfigurationManager.AppSettings["url"];

            try
            {
                using (var wb = new WebClient())
                {
                    var data = new NameValueCollection
                    {
                        ["emisora"] = ConfigurationManager.AppSettings["emisora"],
                        ["fecha"] = ayer.ToString("dd-MM-yyyy"),
                        ["avisos"] = avisos.ToString(),
                        ["primerPlay"] = primerPlay.ToString(),
                        ["ultimoPlay"] = ultimoPlay.ToString(),
                        ["tandasLocales"] = tandasLocales.ToString(),
                        ["cuentaTandas"] = cuentaTandas.ToString(),
                        ["totalRelleno"] = totalRelleno.ToString(),
                        ["promedioRelleno"] = (totalRelleno / cuentaTandas).ToString("N2"),
                        ["mayorRelleno"] = horaMayorRelleno.ToString(),
                        ["duracionMRelleno"] = mayorRelleno.ToString(),
                        ["correo"] = ConfigurationManager.AppSettings["correo"]
                    };

                    var response = wb.UploadValues(url, "POST", data);
                    Console.WriteLine(Encoding.UTF8.GetString(response));
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("----ERROR----" + ex.Message);
            }

            Console.ReadLine();
        }
    }
}


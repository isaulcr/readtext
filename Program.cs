using Tesseract;
using MySqlConnector;
using System;
using System.Drawing;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using Patagames.Ocr.Enums;
using Patagames.Ocr;

namespace readtext
{
    class Program
    {
        private static string cs = @"server=184.175.93.196;userid=eluucle2_isaul;password=!!Rainbow123!!;database=eluucle2_inventario";

        static void Main(string[] args)
        {
            using var con = new MySqlConnection(cs);

            //iniciando lectura de launcher
            int milliseconds = 300000;


            while (true)
            {
                GetCalculos();
                Thread.Sleep(milliseconds);
            }

            async void GetCalculos()
            {
                double porcAnterior = GetPorcentajeBD();
                double diferencia = 0;
                double porcentaje_restante = 0;
                string porcLauncher = await GetPorcentaje();
                string minutos_restantes = "0";
                porcLauncher = Regex.Replace(porcLauncher, "[.]", ",");
                porcLauncher = Regex.Replace(porcLauncher, "[n]", string.Empty);
                porcLauncher = Regex.Replace(porcLauncher, "[%]", string.Empty);

                if (porcLauncher != "" && float.Parse(porcLauncher) != 100)
                {
                    try
                    {
                        float porcentaje_actual = float.Parse(porcLauncher);

                        if (porcAnterior != porcentaje_actual)
                        {
                            if (porcentaje_actual == 0)
                            {
                                porcAnterior = 0;
                            }
                            porcentaje_restante = 100 - porcentaje_actual;
                            string porcRestante = porcentaje_restante.ToString();
                          
                            porcentaje_restante = double.Parse(porcRestante);

                            diferencia = (porcentaje_actual - porcAnterior);
                            diferencia = diferencia / 5;
                            double minutos = (100 - porcentaje_actual) / diferencia;

                            TimeSpan result = TimeSpan.FromMinutes(minutos);
                            minutos_restantes = result.ToString("hh':'mm");
                            string hora_salida = "2023-10-30 02:25:34";



                            UpdateData(porcAnterior, porcentaje_actual, porcentaje_restante, minutos_restantes);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                Console.WriteLine($"Porcentaje de Launcher: {porcLauncher}");
                Console.WriteLine($"Porcentaje de BD: {porcAnterior}");
                Console.WriteLine($"Porcentaje restante: {porcentaje_restante}");
                Console.WriteLine($"Minutos restantes: {minutos_restantes}");
            }

            async Task<string> GetPorcentaje()
            {
                var url = "https://patch.dboglobal.to:5000/bossProgress";

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(url);

                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string strResult = await response.Content.ReadAsStringAsync();

                        return strResult;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            double GetPorcentajeBD()
            {
                double porcAnterior = 0.0;
                //leer ultimo %
                con.Open();
                string sql = "SELECT * FROM DBO_dragon";
                using var cmd = new MySqlCommand(sql, con);
                using MySqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    porcAnterior = double.Parse(rdr[1].ToString());
                }
                con.Close();
                return porcAnterior;
            }

            bool UpdateData(double porcAnterior, float porcentaje_actual, double porcentaje_restante, string minutos_restantes)
            {
                string[] tiempo = minutos_restantes.Split(":");

                DateTime actual = DateTime.Now;

                string peru = actual.AddHours(Int32.Parse(tiempo[0]) - 2).AddMinutes(Int32.Parse(tiempo[1])).ToString("t");
                string ven = actual.AddHours(Int32.Parse(tiempo[0]) - 1).AddMinutes(Int32.Parse(tiempo[1])).ToString("t");
                string chile = actual.AddHours(Int32.Parse(tiempo[0])).AddMinutes(Int32.Parse(tiempo[1])).ToString("t");

                if (porcAnterior.ToString() != porcentaje_actual.ToString())
                {
                    try
                    {
                        con.Open();

                        string stm = "Update DBO_dragon set porcentaje_anterior = '" + porcAnterior
                          + "',porcentaje_actual='" + porcentaje_actual
                          + "',porcentaje_restante='" + porcentaje_restante
                          + "',tiempo_restante='" + minutos_restantes
                          + "',hora_salida_peru = '" + peru + "'"
                          + ",hora_salida_venezuela = '" + ven + "'"
                          + ",hora_salida_chile = '" + chile + "'";


                        MySqlCommand cmdd = new MySqlCommand(stm, con);
                        string version = Convert.ToString(cmdd.ExecuteScalar());
                        return true;

                    }
                    catch (MySqlException ex)
                    {
                        Console.WriteLine("Error: {0}", ex.ToString());
                        return false;
                    }
                    finally
                    {

                        if (con != null)
                        {
                            con.Close();
                        }

                    }
                }
                return false;

            }


        }


    }
}
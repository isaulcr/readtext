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

            Console.WriteLine("Indique coordenada X (115):");
            int X = Int32.Parse(Console.ReadLine().ToString());
            Console.WriteLine("Indique coordenada Y (824):");
            int Y = Int32.Parse(Console.ReadLine().ToString());

            while (true)
            {
                GetCalculos();
                Thread.Sleep(milliseconds);
            }

            void GetCalculos()
            {
                double porcAnterior = GetPorcentajeBD();
                double diferencia = 0;
                double porcentaje_restante = 0;
                string porcLauncher = GetPorcentaje();
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
                            if (porcentaje_restante >= 10)
                            {
                                if (porcRestante.Length > 5)
                                {
                                    porcRestante = porcRestante.Substring(0, 5);
                                    porcLauncher = porcLauncher.Substring(0, 5);
                                }
                            }
                            else
                            {
                                if (porcRestante.Length > 4)
                                {
                                    porcRestante = porcRestante.Substring(0, 4);
                                    porcLauncher = porcLauncher.Substring(0, 4);

                                }
                            }
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

            string GetPorcentaje()
            {
                string porcLauncher = "";
                Console.Clear();
                Console.WriteLine("Leyendo porcentaje desde el launcher...");
                Bitmap memoryImage;
                memoryImage = new Bitmap(93, 39);
                System.Drawing.Size s = new System.Drawing.Size(memoryImage.Width, memoryImage.Height);
                Graphics memoryGraphics = Graphics.FromImage(memoryImage);
                memoryGraphics.CopyFromScreen(X, Y, 0, 0, s);

                string fileName = string.Format(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\imagen.png");

                // Guarda el archivo
                memoryImage.Save(fileName);

                //LEER PORCENTAJE
                string plainText = "";


                var ocrengine = new TesseractEngine(@".\tessdata", "eng", EngineMode.Default);
                ocrengine.SetVariable("tessedit_char_whitelist", "0123456789.");


                var img = Pix.LoadFromFile(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\imagen.png");
                var res = ocrengine.Process(img);
                plainText = res.GetText();
                return plainText;
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
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TSELayoutMappers;

namespace TSELoadCands
{
    public class LoadCands
    {
        public static String[][] ReadCSV(Int32 ano, String uf, String arq, String path)
        {
            List<String[]> campos = new List<String[]>();

            String arquivo = String.Format(path, ano, uf, arq);

            System.IO.StreamReader readerArq = new System.IO.StreamReader(arquivo, Encoding.Default);

            while (!readerArq.EndOfStream)
            {
                String linha = readerArq.ReadLine();
                campos.Add(linha.Split(';'));
            }

            readerArq.Close();

            return campos.ToArray();
        }

        static public String unq(String value)
        {
            return value.Substring(1, value.Length - 2);
        }

        public static void LoadCSV<T>(String[] regiao, TSEMapper[] map, String arquivo, String path)
        {
            PropertyInfo[] props = typeof(T).GetProperties().ToArray();

            DataTable tbl = new DataTable();

            tbl.TableName = "dbo." + typeof(T).Name;

            foreach (PropertyInfo prop in props)
            {
                tbl.Columns.Add(new DataColumn(prop.Name, prop.PropertyType));
            }

            string connection = "Data Source=LONDON50;Initial Catalog=TSEdb;Integrated Security=True";
            SqlConnection con = new SqlConnection(connection);
            con.Open();

            SqlBulkCopy bulkcopy = new SqlBulkCopy(con);
            bulkcopy.DestinationTableName = tbl.TableName;

            int rowCount = 0;

            foreach (String uf in regiao)
            {
                try
                {
                    String[][] campos = ReadCSV(2016, uf, arquivo, path);
                    Console.WriteLine(String.Format("{2} {0}: {1:#,#} ", uf, campos.Length, typeof(T).Name));

                    foreach (String[] linha in campos)
                    {
                        DataRow dr = tbl.NewRow();
                        foreach (DataColumn column in tbl.Columns)
                        {
                            int i = 0;
                            for (; i < map.Count(); i++)
                            {
                                if (map[i].FieldName == column.ColumnName)
                                    break;
                            }

                            if (i >= map.Count()) continue;

                            if (map[i].FieldPos == -1)
                            {
                                dr[map[i].FieldName] = "0";
                            }
                            else
                            {
                                try
                                {
                                    switch (map[i].FieldType)
                                    {
                                        case TSETypes.tseInt:
                                            dr[map[i].FieldName] = unq(linha[map[i].FieldPos]);
                                            break;

                                        case TSETypes.tseLong:
                                            dr[map[i].FieldName] = unq(linha[map[i].FieldPos]);
                                            break;

                                        case TSETypes.tseDouble:
                                            try
                                            {
                                                Double d = Convert.ToDouble(unq(linha[map[i].FieldPos]));
                                                dr[map[i].FieldName] = d;
                                            }
                                            catch (Exception)
                                            {
                                                try
                                                {
                                                    Double d = Convert.ToDouble("0" + unq(linha[map[i].FieldPos]));
                                                    dr[map[i].FieldName] = d;
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.WriteLine(ex.Message);
                                                }
                                            }
                                            break;

                                        case TSETypes.tseDate:
                                            DateTime dt = Convert.ToDateTime(unq(linha[map[i].FieldPos]));
                                            if (dt.Year < 1900) dt = dt.AddYears((19 - dt.Year / 100) * 100);
                                            dr[map[i].FieldName] = dt.ToString("yyyy-MM-ddTHH:mm:ss");
                                            break;

                                        case TSETypes.tseString:
                                            dr[map[i].FieldName] = unq(linha[map[i].FieldPos]).Replace(@"\", @"\\").Replace('\x09', ' ');
                                            break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }
                        }

                        tbl.Rows.Add(dr);
                        if (++rowCount % 10000 == 0)
                        {
                            try
                            {
                                bulkcopy.WriteToServer(tbl);
                                tbl.Clear();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                                tbl.Clear();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro na carga de {0}: {1}", uf, ex.Message);
                }
            }

            try
            {
                bulkcopy.WriteToServer(tbl);
                tbl.Clear();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                tbl.Clear();
            }
            con.Close();
        }

        public static void SumValores(String[] ufs)
        {
            foreach (String uf in ufs)
            {
                Console.WriteLine("SumValores {0}. Hora: {1}", uf, DateTime.Now.ToString());
                string sumSql = "update candidatura " +
                                    "set TotBensValor = VALORES.VALOR " +
                                    "from candidatura join " +
                                        "(select bemcandidato.BemSeqCand SEQ, sum(bemcandidato.BemCandValor) VALOR " +
                                         "from bemcandidato " +
                                         "where bemcandidato.BemCandUF = '" + uf + "' " +
                                         "group by bemcandidato.BemSeqCand) VALORES " +
                                "on candidatura.SeqCand = VALORES.SEQ ";

                //string connection = "server=localhost; uid=root; password=root; database=tseDB; Persist Security Info=True; Pooling=True";
                String connection = "Data Source=LONDON50;Initial Catalog=TSEdb;Integrated Security=True";
                SqlConnection con = new SqlConnection(connection);
                con.Open();
                SqlCommand cmd = new SqlCommand(sumSql, con);
                cmd.ExecuteNonQuery();
                con.Close();
            }
        }

    }
}

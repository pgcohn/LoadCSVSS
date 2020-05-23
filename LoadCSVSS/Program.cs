using System;
using System.Globalization;
using System.Linq;
using System.Threading;

using TSELoadCands;
using TSEdb;
using TSELayoutMappers;

namespace LoadCSVBulk
{
    class Program
    {
        static String[] ufs = {"AC", "AL", "AM", "AP", "BA", "CE", "ES", "GO", "MA", "MG", "MS", "MT",
                               "PA", "PB", "PE", "PI", "PR", "RJ", "RN", "RO", "RR", "RS", "SC", "SE", "SP", "TO" };

        static String[] nordeste = { "AL", "CE", "MA", "PB", "PE", "PI", "RN", "SE" };
        static String[] nortecentrooeste = { "AM", "AP", "PA", "RR", "TO", "AC", "DF", "GO", "MS", "MT", "RO" };
        static String[] sul = { "PR", "RS", "SC" };
        static String[] baesrj = { "BA", "ES", "RJ" };
        static String[] mg = { "MG" };
        static String[] sp = { "SP" };

        static void Main(string[] args)
        {
            CultureInfo culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.NumberFormat.NumberGroupSeparator = ",";
            Thread.CurrentThread.CurrentCulture = culture;

            using (TSEdbContext contextDB = new TSEdbContext())
            {
                try
                {
                    contextDB.Database.CreateIfNotExists();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.ReadKey();
                }
            }
            
            String path = @"C:\Users\Paulo\Documents\Pessoal\Projeto QMR\Eleições {0}\{2}_{0}\{2}_{0}_{1}.txt";

            Maps maps = new Maps();

            DateTime inicio = DateTime.Now;
            Console.WriteLine("Inicio Programa: {0}", inicio.ToString());

            LoadCands.LoadCSV<Candidato>(ufs, maps.CandidatoMap.ToArray(), "consulta_cand", path);
            LoadCands.LoadCSV<Candidatura>(ufs, maps.CandidaturaMap.ToArray(), "consulta_cand", path);
            LoadCands.LoadCSV<BemCandidato>(ufs, maps.BemCandidatoMap.ToArray(), "bem_candidato", path);
            LoadCands.SumValores(ufs);

            TimeSpan tempo = DateTime.Now - inicio;
            Console.WriteLine("Tempo total: {0}", tempo.ToString());
            Console.ReadKey();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Explorer
{
    class Model
    {
        public ImageSource image { get; set; }
        public string name { get; set; }
        public string size { get; set; }
        public string date_of_change { get; set; }
        public string path { get; set; }
        private static object Lock = new object();
        public List<string> GetFiles(string path, string pattern)
        {
            var files = new List<string>();

            try
            {
                files.AddRange(Directory.GetFiles(path, pattern,SearchOption.TopDirectoryOnly));
                foreach (var directory in Directory.EnumerateDirectories(path))
                {
                    files.AddRange(GetFiles(directory, pattern));
                }
                /*Parallel.ForEach(Directory.GetDirectories(path), item => {
                    files.AddRange(GetFiles(item, pattern));
                });*/
            }
            catch (UnauthorizedAccessException) { }

            return files;
        }
    }
}

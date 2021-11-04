using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;

namespace Explorer
{
    class ViewModelExplorer : INotifyPropertyChanged
    {
        Model model = new Model();
        Task task = null;
        ManualResetEvent e = new ManualResetEvent(false);
        CancellationTokenSource cancelTokSrc;
        public static object Lock = new object();
        public ViewModelExplorer()
        {
            GetAllDisk();
        }
        /*private ImageSource _image;
        public ImageSource Image
        {
            get { return _image; }
            set
            {
                _image = value;
                RaisePropertyChanged(nameof(Image));
            }
        }*/

        private int Count = 0;
        private void Search(Object ct)
        {
            CancellationToken cancelTok = (CancellationToken)ct;
            var files = new List<string>();
            try
            {
                if (Subdirectories == true)
                {
                    if (Directory.GetDirectories(SelectedCatalog).Length != 0)
                    {
                        files.AddRange(Directory.GetFiles(SelectedCatalog, mask, SearchOption.TopDirectoryOnly));
                        foreach (var directory in Directory.EnumerateDirectories(SelectedCatalog))
                        {
                            if (cancelTok.IsCancellationRequested)
                                cancelTok.ThrowIfCancellationRequested();
                            e.WaitOne();
                            Parallel.ForEach(files, InsertItems);
                            files = new List<string>();
                            files.AddRange(model.GetFiles(directory, Mask));
                        }
                    }
                    else
                    {
                        if (cancelTok.IsCancellationRequested)
                            cancelTok.ThrowIfCancellationRequested();
                        e.WaitOne();
                        files.AddRange(Directory.GetFiles(SelectedCatalog, mask, SearchOption.TopDirectoryOnly));
                        Parallel.ForEach(files, InsertItems);
                    }
                }
                else if (Subdirectories == false)
                {
                    if (cancelTok.IsCancellationRequested)
                        cancelTok.ThrowIfCancellationRequested();
                    e.WaitOne();
                    files.AddRange(Directory.GetFiles(SelectedCatalog, mask, SearchOption.TopDirectoryOnly));

                    Parallel.ForEach(files, InsertItems);
                }
                e.Reset();
                TextButtonSearch = "Найти";
                MessageBox.Show("Готово!");
            }
            catch (UnauthorizedAccessException) { }
        }
        private ImageSource ExtractIcon(string item)
        {
            ImageSource source = null;
            Icon icon = Icon.ExtractAssociatedIcon(item);
            if (icon != null)
            {
                using (var bmp = icon.ToBitmap())
                {
                    var stream = new MemoryStream();
                    bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    source = BitmapFrame.Create(stream);
                }
            }
            return source;
        }
        private void InsertItems(string item, ParallelLoopState state)
        {
            e.WaitOne();
            if (cancelTokSrc.Token.IsCancellationRequested)
            {
                cancelTokSrc.Token.ThrowIfCancellationRequested();
                state.Break();
            }
            else
            {
                Model model1 = new Model();
                FileInfo info;
                info = new FileInfo(item);
                ImageSource imageSource = null;

                model1.name = info.Name;
                model1.size = info.Length.ToString() + " байт";
                model1.date_of_change = info.LastWriteTime.ToString();
                model1.path = item;

                if (info.Extension == ".jpg" || info.Extension == ".png")
                {
                    try
                    {
                        var converter = new ImageSourceConverter();
                        imageSource = converter.ConvertFromInvariantString(item) as ImageSource;
                    }
                    catch (NotSupportedException)
                    {
                        imageSource = ExtractIcon(item);
                    }
                }
                else
                    imageSource = ExtractIcon(item);
                model1.image = imageSource;
                Application.Current.Dispatcher.Invoke(() => AllFiles.Add(model1));
                lock (Lock)
                    Count++;
                QuantityText = "Элементов : " + Count.ToString();
            }
        }
        private List<string> GetFiles(string path, string pattern)
        {
            var files = new List<string>();
            try
            {
                files.AddRange(Directory.GetFiles(path, pattern));
                foreach (var directory in Directory.GetDirectories(path))
                    files.AddRange(GetFiles(directory, pattern));
            }
            catch (UnauthorizedAccessException) { }

            return files;
        }
        private void GetAllDisk()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo item in allDrives)
                AllDisk.Add(item.Name);
        }
        #region Collection
        private ObservableCollection<Model> all_files = new ObservableCollection<Model>();
        public ObservableCollection<Model> AllFiles
        {
            get { return all_files; }
            set
            {
                all_files = value;
                RaisePropertyChanged(nameof(AllFiles));
            }
        }
        private ObservableCollection<string> all_disk = new ObservableCollection<string>();
        public ObservableCollection<string> AllDisk
        {
            get { return all_disk; }
            set
            {
                all_disk = value;
                RaisePropertyChanged(nameof(AllDisk));
            }
        }
        #endregion
        #region Properties
        private string mask = "*.";
        public string Mask
        {
            get { return mask; }
            set
            {
                string temp = mask;
                mask = value;
                if (mask != temp)
                {
                    TextButtonSearch = "Найти";
                    cancelTokSrc.Cancel();
                    e.Reset();
                }
                RaisePropertyChanged(nameof(Mask));
            }
        }
        private string selected_catalog = "";
        public string SelectedCatalog
        {
            get { return selected_catalog; }
            set
            {
                string temp = selected_catalog;
                selected_catalog = value;
                if (selected_catalog != temp)
                {
                    TextButtonSearch = "Найти";
                    cancelTokSrc.Cancel();
                    e.Reset();
                }
                RaisePropertyChanged(nameof(SelectedCatalog));
            }
        }
        private bool subdirectories = true;
        public bool Subdirectories
        {
            get { return subdirectories; }
            set
            {
                subdirectories = value;
                TextButtonSearch = "Найти";
                cancelTokSrc.Cancel();
                e.Reset();
                RaisePropertyChanged(nameof(Subdirectories));
            }
        }
        private string quantity_text = "Элементов : 0";
        public string QuantityText
        {
            get { return quantity_text; }
            set
            {
                quantity_text = value;
                RaisePropertyChanged(nameof(QuantityText));
            }
        }
        private string serch_catalog;
        public string SerchCatalog
        {
            get { return serch_catalog; }
            set
            {
                serch_catalog = value;
                RaisePropertyChanged(nameof(SerchCatalog));
            }
        }
        private string text_button_search = "Найти";
        public string TextButtonSearch
        {
            get { return text_button_search; }
            set
            {
                text_button_search = value;
                RaisePropertyChanged(nameof(TextButtonSearch));
            }
        }
        #endregion
        #region DelegateCommand
        private DelegateCommand _Command;
        public ICommand LabelClickOptions
        {
            get
            {
                if (_Command == null)
                {
                    _Command = new DelegateCommand(Execute, CanExecutes);
                }
                return _Command;
            }
        }
        private void Execute(object o)
        {
            if (TextButtonSearch == "Найти")
            {
                TextButtonSearch = "Стоп";
                if (task != null)
                {
                    cancelTokSrc.Cancel();
                }
                e.Set();
                AllFiles.Clear();
                Count = 0;
                cancelTokSrc = new CancellationTokenSource();
                task = Task.Factory.StartNew(Search, cancelTokSrc.Token);
            }
            else if (TextButtonSearch == "Стоп")
            {
                TextButtonSearch = "Пуск";
                e.Reset();
            }
            else if (TextButtonSearch == "Пуск")
            {
                TextButtonSearch = "Стоп";
                e.Set();
            }
        }
        private bool CanExecutes(object o)
        {
            if (String.IsNullOrEmpty(SelectedCatalog) || String.IsNullOrEmpty(Mask))
            {
                return false;
            }
            return true;
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

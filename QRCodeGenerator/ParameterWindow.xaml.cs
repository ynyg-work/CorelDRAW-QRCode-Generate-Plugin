using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using corel = Corel.Interop.VGCore;

namespace QRCodeGenerator
{
    /// <summary>
    /// ParameterWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ParameterWindow
    {
        // 后台工作对象
        private readonly BackgroundWorker _worker;

        // CorelIDRAW实例对象
        private corel.Application _corelApp;

        public ParameterWindow(corel.Application app)
        {
            InitializeComponent();
            // 获取CorelIDRAW实例对象
            _corelApp = app;

            // 1. 创建并设置属性
            _worker = new BackgroundWorker
            {
                // 允许在后台任务里使用 ReportProgress(percent) 向 UI 报告进度
                WorkerReportsProgress = true,
                // 允许调用 CancelAsync() 来请求取消任务
                WorkerSupportsCancellation = true
            };

            // 2. 注册三个关键事件
            _worker.DoWork += Worker_DoWork;
            //   → 任务真正要做的工作写在这个事件处理器里，
            //     它运行在后台线程，UI 不会被阻塞。

            _worker.ProgressChanged += Worker_ProgressChanged;
            //   → 当后台线程调用 ReportProgress(...) 时，
            //     这里会被触发，且始终运行在 UI 线程，
            //     你可以在这里更新 ProgressBar、Label 等控件。

            _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            //   → DoWork 结束后（正常完成、取消或抛异常都会触发），
            //     在此事件里你可以恢复按钮状态、弹出提示、处理异常等，
            //     同样运行在 UI 线程。
        }

        /// <summary>
        /// 点击选择文件按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开文件选择弹窗，只能选择txt
            var openFileDialog = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt" };
            // 如果选择了文件，则将文件路径显示在文本框中
            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        /// <summary>
        /// 验证文本输入以确保它仅包含数字
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件数据</param>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null) e.Handled = !int.TryParse(textBox.Text + e.Text, out _);
        }

        /// <summary>
        /// 开始生成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            // 2. 参数校验
            if (!int.TryParse(SizeTextBox.Text, out int size) ||
                !int.TryParse(MarginTextBox.Text, out int margin) ||
                !int.TryParse(MaxPerRowTextBox.Text, out int maxPerRow) ||
                string.IsNullOrWhiteSpace(FilePathTextBox.Text))
            {
                MessageBox.Show("请先输入正确的文件路径及数字参数。", "参数错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. 禁用按钮、显示进度条
            GenerateButton.IsEnabled = false;
            BrowseButton.IsEnabled = false;
            ProgressBar.Value = 0;
            ProgressBar.Visibility = Visibility.Visible;

            // 4. 通过 RunWorkerAsync 传递所有参数
            var args = new GenerateArgs
            {
                FilePath = FilePathTextBox.Text,
                QrSize = size,
                Margin = margin,
                MaxPerRow = maxPerRow
            };
            _worker.RunWorkerAsync(args);
        }

        /// <summary>
        /// 取消生成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_worker.IsBusy) _worker.CancelAsync();
            Close();
        }

        /// <summary>
        /// 后台执行生成二维码的逻辑
        /// </summary>
        /// <param name="sender">触发事件的对象</param>
        /// <param name="e">包含任务参数和状态的事件数据</param>
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // 接收参数
            var args = (GenerateArgs)e.Argument;
            // 读取文件
            List<string> rows = Utils.ReadTxtLines(args.FilePath);
            // 总数量
            int totalRows = rows.Count;
            // 当前数量
            int count = 0;

            // x
            int x = 0;
            // y
            int y = 0;

            // 获取当前文档
            corel.Document document = _corelApp.ActiveDocument;

            // 设置文档的单位为毫米
            document.Unit = corel.cdrUnit.cdrMillimeter;

            // 获取当前图层
            corel.Layer layer = document.ActiveLayer;

            // 循环每一行
            foreach (string row in rows)
            {
                // 如果用户取消了任务
                if (_worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                // 生成二维码
                Utils.PlaceQrCode(
                    args.QrSize,
                    row,
                    x,
                    y,
                    document,
                    layer
                );
                
                // 完成数量+1
                count++;

                // 计算x和y
                if (count % args.MaxPerRow == 0)
                {
                    y -= args.QrSize + args.Margin;
                    x = 0;
                }
                else
                    x += args.QrSize + args.Margin;

                // 汇报进度
                int percent = count * 100 / totalRows;
                _worker.ReportProgress(percent);
            }
        }

        /// <summary>
        /// 更新生成二维码过程中的进度信息
        /// </summary>
        /// <param name="sender">触发事件的对象</param>
        /// <param name="e">包含进度百分比的事件数据</param>
        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        /// <summary>
        /// 后台任务完成时触发
        /// </summary>
        /// <param name="sender">事件的触发者</param>
        /// <param name="e">存储事件数据的 RunWorkerCompletedEventArgs 对象</param>
        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgressBar.Visibility = Visibility.Collapsed;
            GenerateButton.IsEnabled = true;
            BrowseButton.IsEnabled = true;

            if (e.Cancelled)
                MessageBox.Show("操作已取消。", "取消", MessageBoxButton.OK, MessageBoxImage.Information);
            else if (e.Error != null)
                MessageBox.Show($"生成失败：{e.Error.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            else
                MessageBox.Show("二维码生成完成！", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 表示生成参数的类，用于封装生成二维码过程中的相关配置信息。
        /// </summary>
        private class GenerateArgs
        {
            public string FilePath { get; set; }
            public int QrSize { get; set; }
            public int Margin { get; set; }
            public int MaxPerRow { get; set; }
        }
    }
}
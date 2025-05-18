using System.Windows;
using QRCodeGenerator.Resources;
using corel = Corel.Interop.VGCore;

namespace QRCodeGenerator
{
    /// <summary>
    /// ShowWindowBtn.xaml 的交互逻辑
    /// </summary>
    public partial class ShowWindowBtn
    {
        // CorelIDRAW实例对象
        private corel.Application _corelApp;

        public ShowWindowBtn(object app)
        {
            _corelApp = app as corel.Application;
            InitializeComponent();
        }

        /// <summary>
        /// 点击按钮触发
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件数据</param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // 如果没有活动中的文档
            if (!HasActiveDocument())
            {
                // 从 Resources.Strings 里拿文本
                var msg = Strings.Msg_NoDoc;

                // MessageBoxButton.OKCancel 在中文环境下默认显示 “确定”/“取消”
                var result = MessageBox.Show(
                    msg,
                    Strings.Title_MainWindow,
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.OK)
                {
                    // 用户点击 “确定”
                    CreateNewDocument();
                }
                else
                {
                    return;
                }
            }

            // 创建窗口
            CreateParameterWindow();
        }


        /// <summary>
        /// 检查是否有活动的文档
        /// </summary>
        private bool HasActiveDocument()
        {
            return _corelApp != null && _corelApp.ActiveDocument != null;
        }

        /// <summary>
        /// 根据需要创建一个新文档
        /// </summary>
        private void CreateNewDocument()
        {
            _corelApp.CreateDocument();
        }

        /// <summary>
        /// 创建参数窗口
        /// </summary>
        private void CreateParameterWindow()
        {
            // 创建并显示二维码生成窗口（模态对话框）
            var qrCodeWindow = new ParameterWindow(_corelApp); // 替换为您的实际窗口类名
            qrCodeWindow.Owner = Window.GetWindow(this); // 设置父窗口
            qrCodeWindow.ShowDialog(); // 显示为模态窗口
        }
    }
}
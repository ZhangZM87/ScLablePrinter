using System.Windows;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using HandyControl;
using SCLabelPrinter.Core.Printing;
using SCLabelPrinter.Core.Printers;
using SCLabelPrinter.Core.Serialization;
using SCLabelPrinter.Core.Storage;
using SCLabelPrinter.Infrastructure.Native;
using SCLabelPrinter.Infrastructure.Printers;
using SCLabelPrinter.Infrastructure.Storage;
using SCLabelPrinter.Infrastructure.Excel;
using SCLabelPrinter.Core.Services;
using SCLabelPrinter.Core.Export;
using SCLabelPrinter.Core.Export.Tspl;
using SCLabelPrinter.Services;
using SCLabelPrinter.ViewModels;

namespace SCLabelPrinter;

/// <summary>
/// 提供应用启动、依赖注入和主窗口创建逻辑。
/// </summary>
public partial class App : Application
{
	private ServiceProvider? _serviceProvider;

	/// <summary>
	/// 在应用启动时初始化编码支持与依赖注入容器。
	/// </summary>
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		var services = new ServiceCollection();
		ConfigureServices(services);
		_serviceProvider = services.BuildServiceProvider();

		var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
		MainWindow = mainWindow;
		mainWindow.Show();
	}

	/// <summary>
	/// 在应用退出时释放依赖注入容器。
	/// </summary>
	protected override void OnExit(ExitEventArgs e)
	{
		_serviceProvider?.Dispose();
		base.OnExit(e);
	}

	/// <summary>
	/// 注册应用运行所需的服务、视图模型和窗口实例。
	/// </summary>
	private static void ConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<StatusCenter>();
		services.AddSingleton<IUserNotificationService, HandyControlNotificationService>();
		services.AddSingleton<IFileDialogService, FileDialogService>();

		services.AddSingleton<IElementTsplWriter, TextElementTsplWriter>();
		services.AddSingleton<IElementTsplWriter, BarcodeElementTsplWriter>();
		services.AddSingleton<IElementTsplWriter, QrCodeElementTsplWriter>();
		services.AddSingleton<IElementTsplWriter, BoxElementTsplWriter>();
		services.AddSingleton<IElementTsplWriter, LineElementTsplWriter>();
		services.AddSingleton<IElementTsplWriter, EraseElementTsplWriter>();
        services.AddSingleton<IElementTsplWriter, TableElementTsplWriter>();
		services.AddSingleton<TsplGenerator>();
		services.AddSingleton<ITsplParser, TsplParser>();
		services.AddSingleton<ITsplInputAnalyzer, TsplInputAnalyzer>();
		services.AddSingleton<PrintDataChunker>();
		services.AddSingleton<PrinterStatusInterpreter>();

		services.AddSingleton<UsbInterop>();
		services.AddSingleton<IPrinterService, UsbPrinterService>();
		services.AddSingleton<LabelTemplateSerializer>();
		services.AddSingleton<ILabelTemplateStorageService, LabelTemplateStorageService>();
		services.AddSingleton<IPrintFileService, PrintFileService>();

		services.AddSingleton<IExcelImportService, ClosedXmlExcelImportService>();

		// Export layer
		services.AddSingleton<ILabelExporter, TsplExporter>();
		services.AddSingleton<IExporterFactory, ExporterFactory>();

		services.AddSingleton<PrinterViewModel>();
		services.AddSingleton<EditorViewModel>();
		services.AddSingleton<FilePrintViewModel>();
		services.AddSingleton<MainViewModel>();
		services.AddSingleton<MainWindow>();
	}
}


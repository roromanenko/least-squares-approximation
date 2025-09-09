using System;
using System.Windows;

namespace UI
{
	/// <summary>
	/// Логика взаимодействия для App.xaml
	/// </summary>
	public partial class App : Application
	{
		/// <summary>
		/// Обработчик запуска приложения
		/// </summary>
		protected override void OnStartup(StartupEventArgs e)
		{
			try
			{
				base.OnStartup(e);
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Ошибка при запуске приложения: {ex.Message}",
					"Ошибка",
					MessageBoxButton.OK,
					MessageBoxImage.Error
				);

				Shutdown(1);
			}
		}

		/// <summary>
		/// Обработчик необработанных исключений
		/// </summary>
		protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
		{
			base.OnSessionEnding(e);
		}

		/// <summary>
		/// Обработчик выхода из приложения
		/// </summary>
		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);
		}
	}
}
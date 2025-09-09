using Core.Approximation;
using Core.Data;
using OxyPlot;
using OxyPlot.Series;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace UI.ViewModels
{
	/// <summary>
	/// ViewModel для главного окна приложения с МНК аппроксимацией
	/// </summary>
	public class MainViewModel : INotifyPropertyChanged
	{
		private PlotModel _plotModel;
		private DataSets.DataSetInfo _selectedDataSet;
		private int _approximationPoints = 200;
		private readonly LeastSquaresFitter _fitter;
		private LeastSquaresFitter.PolynomialResult _currentResult;

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Модель графика OxyPlot
		/// </summary>
		public PlotModel PlotModel
		{
			get => _plotModel;
			set
			{
				_plotModel = value;
				OnPropertyChanged(nameof(PlotModel));
			}
		}

		/// <summary>
		/// Коллекция доступных наборов данных
		/// </summary>
		public ObservableCollection<DataSets.DataSetInfo> DataSets { get; }

		/// <summary>
		/// Выбранный набор данных
		/// </summary>
		public DataSets.DataSetInfo SelectedDataSet
		{
			get => _selectedDataSet;
			set
			{
				_selectedDataSet = value;
				OnPropertyChanged(nameof(SelectedDataSet));
				UpdatePlot();
			}
		}

		/// <summary>
		/// Количество точек для построения аппроксимирующей кривой
		/// </summary>
		public int ApproximationPoints
		{
			get => _approximationPoints;
			set
			{
				if (value > 10 && value <= 1000)
				{
					_approximationPoints = value;
					OnPropertyChanged(nameof(ApproximationPoints));
				}
			}
		}

		/// <summary>
		/// Коэффициент A (константа)
		/// </summary>
		public string CoefficientA => _currentResult?.A.ToString("F6") ?? "—";

		/// <summary>
		/// Коэффициент B (при x)
		/// </summary>
		public string CoefficientB => _currentResult?.B.ToString("F6") ?? "—";

		/// <summary>
		/// Коэффициент C (при x²)
		/// </summary>
		public string CoefficientC => _currentResult?.C.ToString("F6") ?? "—";

		/// <summary>
		/// Коэффициент детерминации R²
		/// </summary>
		public string RSquared => _currentResult?.RSquared.ToString("F4") ?? "—";

		/// <summary>
		/// Среднеквадратичная ошибка
		/// </summary>
		public string RMSE => _currentResult?.RootMeanSquareError.ToString("F6") ?? "—";

		/// <summary>
		/// Уравнение полинома в текстовом виде
		/// </summary>
		public string PolynomialEquation => _currentResult?.ToString() ?? "Уравнение не вычислено";

		/// <summary>
		/// Команда для обновления графика
		/// </summary>
		public ICommand UpdatePlotCommand { get; }

		/// <summary>
		/// Конструктор ViewModel
		/// </summary>
		public MainViewModel()
		{
			_fitter = new LeastSquaresFitter();

			// Инициализация наборов данных
			DataSets = new ObservableCollection<DataSets.DataSetInfo>(
				Core.Data.DataSets.GetAllDataSets()
			);

			// Установка начального набора данных
			if (DataSets.Any())
			{
				_selectedDataSet = DataSets.First();
			}

			// Инициализация команд
			UpdatePlotCommand = new RelayCommand(UpdatePlot);

			// Создание начального графика
			InitializePlotModel();
			UpdatePlot();
		}

		/// <summary>
		/// Инициализирует модель графика
		/// </summary>
		private void InitializePlotModel()
		{
			PlotModel = new PlotModel
			{
				Title = "Аппроксимация данных методом наименьших квадратов",
				Background = OxyColors.White
			};

			// Настройка осей
			PlotModel.Axes.Add(new OxyPlot.Axes.LinearAxis
			{
				Position = OxyPlot.Axes.AxisPosition.Bottom,
				Title = "X",
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineColor = OxyColors.LightGray,
				MinorGridlineStyle = LineStyle.Dot,
				MinorGridlineColor = OxyColors.LightGray
			});

			PlotModel.Axes.Add(new OxyPlot.Axes.LinearAxis
			{
				Position = OxyPlot.Axes.AxisPosition.Left,
				Title = "Y",
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineColor = OxyColors.LightGray,
				MinorGridlineStyle = LineStyle.Dot,
				MinorGridlineColor = OxyColors.LightGray
			});
		}

		/// <summary>
		/// Обновляет график с новыми данными
		/// </summary>
		private void UpdatePlot()
		{
			if (SelectedDataSet?.Points == null || !SelectedDataSet.Points.Any())
				return;

			try
			{
				// Выполняем аппроксимацию методом МНК
				_currentResult = _fitter.Fit(SelectedDataSet.Points);

				// Уведомляем об изменении коэффициентов
				OnPropertyChanged(nameof(CoefficientA));
				OnPropertyChanged(nameof(CoefficientB));
				OnPropertyChanged(nameof(CoefficientC));
				OnPropertyChanged(nameof(RSquared));
				OnPropertyChanged(nameof(RMSE));
				OnPropertyChanged(nameof(PolynomialEquation));

				// Очищаем существующие серии
				PlotModel.Series.Clear();

				// Обновляем заголовок
				PlotModel.Title = $"МНК аппроксимация: {SelectedDataSet.Name} (R² = {_currentResult.RSquared:F4})";

				// Добавляем исходные точки (красные)
				var originalSeries = new ScatterSeries
				{
					Title = "Экспериментальные данные",
					MarkerType = MarkerType.Circle,
					MarkerSize = 8,
					MarkerFill = OxyColors.Red,
					MarkerStroke = OxyColors.DarkRed,
					MarkerStrokeThickness = 2
				};

				foreach (var point in SelectedDataSet.Points)
				{
					originalSeries.Points.Add(new ScatterPoint(point.X, point.Y));
				}

				PlotModel.Series.Add(originalSeries);

				// Строим аппроксимирующий полином (синяя линия)
				double minX = SelectedDataSet.Points.Min(p => p.X);
				double maxX = SelectedDataSet.Points.Max(p => p.X);

				// Расширяем диапазон на 10% для лучшей визуализации
				double range = maxX - minX;
				double extendedMinX = minX - range * 0.1;
				double extendedMaxX = maxX + range * 0.1;

				var polynomialPoints = _fitter.GeneratePolynomialPoints(
					_currentResult,
					extendedMinX,
					extendedMaxX,
					ApproximationPoints
				);

				var approximationSeries = new LineSeries
				{
					Title = $"Полином 2-й степени (R² = {_currentResult.RSquared:F3})",
					Color = OxyColors.Blue,
					StrokeThickness = 2,
				};

				foreach (var point in polynomialPoints)
				{
					approximationSeries.Points.Add(new DataPoint(point.X, point.Y));
				}

				PlotModel.Series.Add(approximationSeries);

				// Добавляем серию с остатками (отклонения)
				var residualSeries = new LineSeries
				{
					Title = "Остатки",
					Color = OxyColors.Orange,
					StrokeThickness = 1,
					LineStyle = LineStyle.Dash
				};

				foreach (var point in SelectedDataSet.Points)
				{
					double predictedY = _currentResult.EvaluateAt(point.X);
					residualSeries.Points.Add(new DataPoint(point.X, point.Y));
					residualSeries.Points.Add(new DataPoint(point.X, predictedY));
					residualSeries.Points.Add(new DataPoint(double.NaN, double.NaN)); // Разрыв линии
				}

				PlotModel.Series.Add(residualSeries);

				// Обновляем график
				PlotModel.InvalidatePlot(true);
			}
			catch (Exception ex)
			{
				// В реальном приложении здесь должна быть обработка ошибок
				System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении графика: {ex.Message}");

				// Сбрасываем результат
				_currentResult = null;
				OnPropertyChanged(nameof(CoefficientA));
				OnPropertyChanged(nameof(CoefficientB));
				OnPropertyChanged(nameof(CoefficientC));
				OnPropertyChanged(nameof(RSquared));
				OnPropertyChanged(nameof(RMSE));
				OnPropertyChanged(nameof(PolynomialEquation));
			}
		}

		/// <summary>
		/// Уведомляет об изменении свойства
		/// </summary>
		/// <param name="propertyName">Имя свойства</param>
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	/// <summary>
	/// Простая реализация ICommand для RelayCommand
	/// </summary>
	public class RelayCommand : ICommand
	{
		private readonly Action _execute;
		private readonly Func<bool> _canExecute;

		public event EventHandler CanExecuteChanged;

		public RelayCommand(Action execute, Func<bool> canExecute = null)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter)
		{
			return _canExecute?.Invoke() ?? true;
		}

		public void Execute(object parameter)
		{
			_execute();
		}

		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
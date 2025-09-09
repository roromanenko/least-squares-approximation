using Core.Models;

namespace Core.Approximation
{
	public class LeastSquaresFitter
	{
		// Результат аппроксимации полиномом второго порядка
		public class PolynomialResult
		{
			public double A { get; set; }

			public double B { get; set; }

			public double C { get; set; }

			// Коэффициент детерминации R²
			public double RSquared { get; set; }

			// Среднеквадратичная ошибка
			public double RootMeanSquareError { get; set; }

			// Вычисляет значение полинома для заданного x
			public double EvaluateAt(double x)
			{
				return A + B * x + C * x * x;
			}

			// Возвращает строковое представление полинома
			public override string ToString()
			{
				var terms = new List<string>();

				// Константа a
				if (Math.Abs(A) > 1e-10)
					terms.Add($"{A:F4}");
				else if (terms.Count == 0)
					terms.Add("0");

				// Член bx
				if (Math.Abs(B) > 1e-10)
				{
					if (B > 0 && terms.Count > 0)
						terms.Add($"+ {B:F4}x");
					else if (B < 0)
						terms.Add($"- {Math.Abs(B):F4}x");
					else
						terms.Add($"{B:F4}x");
				}

				// Член cx²
				if (Math.Abs(C) > 1e-10)
				{
					if (C > 0 && terms.Count > 0)
						terms.Add($"+ {C:F4}x²");
					else if (C < 0)
						terms.Add($"- {Math.Abs(C):F4}x²");
					else
						terms.Add($"{C:F4}x²");
				}

				return "y = " + string.Join(" ", terms);
			}
		}

		// Выполняет аппроксимацию данных полиномом второго порядка методом наименьших квадратов
		public PolynomialResult Fit(IEnumerable<Point2D> points)
		{
			if (points == null)
				throw new ArgumentNullException(nameof(points));

			var pointsArray = points.ToArray();
			if (pointsArray.Length < 3)
				throw new ArgumentException("Для аппроксимации полиномом второй степени необходимо минимум 3 точки", nameof(points));

			int n = pointsArray.Length;

			// Составляем систему нормальных уравнений для полинома y = a + bx + cx²
			// Матрица A: [n, Σx, Σx²; Σx, Σx², Σx³; Σx², Σx³, Σx⁴]
			// Вектор b: [Σy, Σxy, Σx²y]

			double sumX = 0, sumX2 = 0, sumX3 = 0, sumX4 = 0;
			double sumY = 0, sumXY = 0, sumX2Y = 0;

			// Вычисляем необходимые суммы
			foreach (var point in pointsArray)
			{
				double x = point.X;
				double y = point.Y;
				double x2 = x * x;
				double x3 = x2 * x;
				double x4 = x3 * x;

				sumX += x;
				sumX2 += x2;
				sumX3 += x3;
				sumX4 += x4;
				sumY += y;
				sumXY += x * y;
				sumX2Y += x2 * y;
			}

			// Составляем матрицу системы и вектор правых частей
			double[,] matrix = {
				{ n, sumX, sumX2 },
				{ sumX, sumX2, sumX3 },
				{ sumX2, sumX3, sumX4 }
			};

			double[] rightSide = { sumY, sumXY, sumX2Y };

			// Решаем систему методом Гаусса
			var coefficients = SolveLinearSystem(matrix, rightSide);

			// Вычисляем статистические показатели
			var result = new PolynomialResult
			{
				A = coefficients[0],
				B = coefficients[1],
				C = coefficients[2]
			};

			CalculateStatistics(pointsArray, result);

			return result;
		}

		/// <summary>
		/// Генерирует точки аппроксимирующего полинома
		/// </summary>
		/// <param name="result">Результат аппроксимации</param>
		/// <param name="minX">Минимальное значение X</param>
		/// <param name="maxX">Максимальное значение X</param>
		/// <param name="numberOfPoints">Количество точек</param>
		/// <returns>Массив точек полинома</returns>
		public Point2D[] GeneratePolynomialPoints(PolynomialResult result, double minX, double maxX, int numberOfPoints = 200)
		{
			if (result == null)
				throw new ArgumentNullException(nameof(result));
			if (numberOfPoints <= 0)
				throw new ArgumentException("Количество точек должно быть положительным", nameof(numberOfPoints));
			if (maxX <= minX)
				throw new ArgumentException("maxX должно быть больше minX");

			var points = new Point2D[numberOfPoints];
			double step = (maxX - minX) / (numberOfPoints - 1);

			for (int i = 0; i < numberOfPoints; i++)
			{
				double x = minX + i * step;
				double y = result.EvaluateAt(x);
				points[i] = new Point2D(x, y);
			}

			return points;
		}

		// Решает систему линейных уравнений методом Гаусса
		private double[] SolveLinearSystem(double[,] matrix, double[] rightSide)
		{
			int n = rightSide.Length;
			double[,] augmentedMatrix = new double[n, n + 1];

			// Формируем расширенную матрицу
			for (int i = 0; i < n; i++)
			{
				for (int j = 0; j < n; j++)
				{
					augmentedMatrix[i, j] = matrix[i, j];
				}
				augmentedMatrix[i, n] = rightSide[i];
			}

			// Прямой ход метода Гаусса
			for (int k = 0; k < n; k++)
			{
				// Поиск главного элемента
				int maxRow = k;
				for (int i = k + 1; i < n; i++)
				{
					if (Math.Abs(augmentedMatrix[i, k]) > Math.Abs(augmentedMatrix[maxRow, k]))
					{
						maxRow = i;
					}
				}

				// Перестановка строк
				if (maxRow != k)
				{
					for (int j = 0; j <= n; j++)
					{
						(augmentedMatrix[k, j], augmentedMatrix[maxRow, j]) = (augmentedMatrix[maxRow, j], augmentedMatrix[k, j]);
					}
				}

				// Проверка на вырожденность
				if (Math.Abs(augmentedMatrix[k, k]) < 1e-12)
				{
					throw new InvalidOperationException("Система уравнений вырождена или плохо обусловлена");
				}

				// Исключение переменных
				for (int i = k + 1; i < n; i++)
				{
					double factor = augmentedMatrix[i, k] / augmentedMatrix[k, k];
					for (int j = k; j <= n; j++)
					{
						augmentedMatrix[i, j] -= factor * augmentedMatrix[k, j];
					}
				}
			}

			// Обратный ход
			double[] solution = new double[n];
			for (int i = n - 1; i >= 0; i--)
			{
				solution[i] = augmentedMatrix[i, n];
				for (int j = i + 1; j < n; j++)
				{
					solution[i] -= augmentedMatrix[i, j] * solution[j];
				}
				solution[i] /= augmentedMatrix[i, i];
			}

			return solution;
		}

		// Вычисляет статистические показатели качества аппроксимации
		private void CalculateStatistics(Point2D[] points, PolynomialResult result)
		{
			if (points.Length == 0) return;

			// Среднее значение Y
			double meanY = points.Average(p => p.Y);

			// Суммы квадратов
			double totalSumSquares = 0;    // Σ(yi - ȳ)²
			double residualSumSquares = 0; // Σ(yi - ŷi)²

			foreach (var point in points)
			{
				double predictedY = result.EvaluateAt(point.X);
				double residual = point.Y - predictedY;

				totalSumSquares += Math.Pow(point.Y - meanY, 2);
				residualSumSquares += residual * residual;
			}

			// Коэффициент детерминации R²
			result.RSquared = totalSumSquares > 1e-12 ? 1.0 - (residualSumSquares / totalSumSquares) : 0.0;

			// Среднеквадратичная ошибка (RMSE)
			result.RootMeanSquareError = Math.Sqrt(residualSumSquares / points.Length);
		}
	}
}
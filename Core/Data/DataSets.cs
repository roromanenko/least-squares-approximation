using Core.Models;

namespace Core.Data
{
	// Статический класс с предопределенными наборами данных для тестирования апроксимации
	public static class DataSets
	{
		// Информация о наборе данных
		public class DataSetInfo
		{
			public string Name { get; set; }
			public string Description { get; set; }
			public Point2D[] Points { get; set; }

			public DataSetInfo(string name, string description, Point2D[] points)
			{
				Name = name;
				Description = description;
				Points = points;
			}
		}

		// Получает все доступные наборы данных
		public static List<DataSetInfo> GetAllDataSets()
		{
			return new List<DataSetInfo>
			{
				new DataSetInfo("MzOmegaZ", "Набор MzOmegaZData", GetMzOmegaZData()),
				new DataSetInfo("MxDeltaH", "Набор MxDeltaHData", GetMxDeltaHData())
			};
		}

		// Наборы точек для апроксимации
		public static Point2D[] GetMzOmegaZData()
		{
			return new Point2D[]
			{
				new Point2D(0.0, -13.0),
				new Point2D(0.2, -13.1),
				new Point2D(0.4, -13.2),
				new Point2D(0.6, -13.7),
				new Point2D(0.8, -14.7),
				new Point2D(0.9, -15.9),
				new Point2D(1.0, -14.2)
			};
		}

		public static Point2D[] GetMxDeltaHData()
		{
			return new Point2D[]
			{
				new Point2D(0.6, -0.0004),
				new Point2D(0.7, -0.000399),
				new Point2D(0.8, -0.000399),
				new Point2D(0.9, -0.00032),
				new Point2D(1.0, -0.00026),
				new Point2D(1.05, -0.000255),
			};
		}
	}
}
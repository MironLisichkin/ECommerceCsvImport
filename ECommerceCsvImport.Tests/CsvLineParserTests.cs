using Xunit;
using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Tests
{
    public class CsvLineParserTests
    {
        // Тест 1: Простая строка без кавычек
        [Fact]
        public void Parse_SimpleLine_ReturnsCorrectFields()
        {
            string line = "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad3-15";

            var result = CsvLineParser.Parse(line);

            Assert.Equal(12, result.Count);
            Assert.Equal("1001", result[0]);
        }

        // Тест 2: Пустые поля
        [Fact]
        public void Parse_EmptyFields_ReturnsEmptyStrings()
        {
            string line = "1011,Блок питания Corsair CV550,,4990,RUB,Компьютеры,9,,true,Corsair,60,CV550";

            var result = CsvLineParser.Parse(line);

            Assert.Equal(12, result.Count);
            Assert.Equal(string.Empty, result[2]); 
            Assert.Equal(string.Empty, result[7]); 
        }

        // Тест 3: Поле с запятой внутри кавычек
        [Fact]
        public void Parse_CommaInsideQuotes_DoesNotSplitField()
        {
            string line = "1012,\"Ноутбук, игровой\",Тестовая строка,59990,RUB,Компьютеры,1,/images/notebook.jpg,true,ASUS,36,VivoBook";

            var result = CsvLineParser.Parse(line);

            Assert.Equal(12, result.Count);
            Assert.Equal("Ноутбук, игровой", result[1]);
        }

        // Тест 4: Экранированные кавычки
        [Fact]
        public void Parse_EscapedQuotes_ReturnsCorrectString()
        {
            string line = "1013,\"Название \"\"Special\"\"\",Описание,100,RUB,Категория,5,/img.jpg,true,Brand,12,Model";

            var result = CsvLineParser.Parse(line);

            Assert.Equal("Название \"Special\"", result[1]);
        }
    }
}

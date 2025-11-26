using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace LabWork
{
    // ====================================================================
    // 1. Клас для роботи з регулярними виразами (Інкапсуляція)
    // ====================================================================
    /// <summary>
    /// Містить методи для пошуку та валідації різних типів даних за допомогою Regular Expressions.
    /// </summary>
    public static class RegexValidator
    {
        // ----------------------------------------------------
        // Патерни для основного завдання (Рівненські номери: XX0000YY)
        // ----------------------------------------------------
        
        // Патерн, що відповідає формату: дві букви, чотири цифри, дві букви.
        private const string PlatePatternFormat = @"\b[A-ZА-Я]{2}\d{4}[A-ZА-Я]{2}\b";
        
        // Коди Рівненської області (ВК, РК)
        private static readonly HashSet<string> RivneCodes = new HashSet<string>
        {
            "ВК", "РК"
        };

        /// <summary>
        /// Знаходить усі номерні знаки Рівненської області у заданому тексті, 
        /// використовуючи регістронезалежний пошук.
        /// </summary>
        /// <param name="text">Вхідний текст для пошуку.</param>
        /// <returns>Список знайдених номерів Рівненської області.</returns>
        public static List<string> FindRivnePlates(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<string>();
            }

            try
            {
                // 1. Пошук усіх збігів, що відповідають загальному формату XX0000YY
                MatchCollection matches = Regex.Matches(
                    text, 
                    PlatePatternFormat, 
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 
                    TimeSpan.FromSeconds(1) // Додавання таймауту для надійності (захист від ReDoS)
                );

                // 2. Фільтрація знайдених збігів за кодом області
                return matches.Cast<Match>()
                              .Select(m => m.Value.ToUpperInvariant())
                              .Where(plate => RivneCodes.Contains(plate.Substring(0, 2)))
                              .ToList();
            }
            catch (RegexMatchTimeoutException ex)
            {
                Console.WriteLine($"❌ Помилка: Час виконання регулярного виразу вичерпано. {ex.Message}");
                return new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Неочікувана помилка при пошуку номерів: {ex.Message}");
                return new List<string>();
            }
        }
        
        // ----------------------------------------------------
        // Патерни для додаткового завдання (Пошук кількох типів)
        // ----------------------------------------------------

        // Словник, що містить різні патерни
        private static readonly Dictionary<string, string> MultiplePatterns = new Dictionary<string, string>
        {
            { "Рівненський Номер", PlatePatternFormat },
            { "Дата (ДД.ММ.РРРР)", @"\b\d{2}\.\d{2}\.\d{4}\b" },
            { "IP-адреса", @"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b" }
        };

        /// <summary>
        /// Шукає одночасно кілька типів шаблонів у тексті.
        /// </summary>
        /// <param name="text">Вхідний текст.</param>
        /// <param name="limit">Максимальна кількість прикладів для повернення на кожен шаблон.</param>
        /// <returns>Словник, де ключ — назва шаблону, а значення — об'єкт з кількістю та прикладами.</returns>
        public static Dictionary<string, (int Count, List<string> Examples)> FindMultiplePatterns(string text, int limit = 3)
        {
            var results = new Dictionary<string, (int Count, List<string> Examples)>();
            
            if (string.IsNullOrEmpty(text))
            {
                return results;
            }

            foreach (var pair in MultiplePatterns)
            {
                string name = pair.Key;
                string pattern = pair.Value;

                try
                {
                    MatchCollection matches = Regex.Matches(
                        text, 
                        pattern, 
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                        TimeSpan.FromSeconds(1)
                    );

                    int count = matches.Count;
                    List<string> examples = matches.Cast<Match>()
                                                   .Select(m => m.Value)
                                                   .Take(limit) // Обмеження кількості прикладів
                                                   .ToList();
                    
                    // Якщо патерн — "Рівненський Номер", застосовуємо додаткову фільтрацію
                    if (name == "Рівненський Номер")
                    {
                        var filteredPlates = examples.Where(plate => RivneCodes.Contains(plate.Substring(0, 2).ToUpperInvariant())).ToList();
                        
                        // Перераховуємо кількість з урахуванням фільтрації ВК/РК
                        var allFilteredPlates = Regex.Matches(text, pattern, RegexOptions.IgnoreCase).Cast<Match>()
                                                     .Select(m => m.Value.ToUpperInvariant())
                                                     .Where(plate => RivneCodes.Contains(plate.Substring(0, 2))).ToList();
                                                     
                        results.Add(name, (allFilteredPlates.Count, filteredPlates));
                    }
                    else
                    {
                        results.Add(name, (count, examples));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Помилка при пошуку патерна '{name}': {ex.Message}");
                    results.Add(name, (0, new List<string> { "Помилка виконання RegEx" }));
                }
            }

            return results;
        }
    }

    // ====================================================================
    // 3. Головна програма
    // ====================================================================
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("## 🔎 Лабораторна робота: Регулярні Вирази (v2.0)\n");

            string inputText = @"
                Тестування: ВК0001АО (Рівне), АА9999КМ (Київ, ігноруємо),
                Дата сьогодні: 26.11.2025. РК1234ВС (Рівне),
                Локальна IP: 192.168.1.10. Ще один номер: BK5555AA.
                Некоректний формат: РК123АВ. Інша дата: 01.01.2000.
                Публічна IP: 203.0.113.45.
            ";

            Console.WriteLine("--- Вхідний текст ---");
            Console.WriteLine(inputText.Trim());
            Console.WriteLine(new string('-', 35));

            // ----------------------------------------------------
            // A. Основне завдання: Пошук номерних знаків Рівненської області
            // ----------------------------------------------------
            Console.WriteLine("### А. Результат основного завдання (Тільки ВК/РК)");
            List<string> rivnePlates = RegexValidator.FindRivnePlates(inputText);

            if (rivnePlates.Any())
            {
                Console.WriteLine($"✅ Знайдено {rivnePlates.Count} номерних знаків Рівненської області:");
                foreach (string plate in rivnePlates)
                {
                    Console.WriteLine($"\t- {plate}");
                }
            }
            else
            {
                Console.WriteLine("❌ Номерних знаків Рівненської області не знайдено.");
            }
            
            Console.WriteLine(new string('-', 35));

            // ----------------------------------------------------
            // Б. Додаткове завдання: Пошук кількох шаблонів
            // ----------------------------------------------------
            Console.WriteLine("### Б. Результат додаткового завдання (Пошук кількох патернів)");
            var multiResults = RegexValidator.FindMultiplePatterns(inputText, 2);

            foreach (var result in multiResults)
            {
                string name = result.Key;
                (int count, List<string> examples) = result.Value;

                Console.WriteLine($"\n📝 {name}: Знайдено {count} збігів.");
                if (examples.Any())
                {
                    Console.WriteLine($"\tПерші {examples.Count} приклади: {string.Join(", ", examples)}");
                }
            }
            
            Console.WriteLine(new string('-', 35));
            Console.WriteLine("Програма завершила роботу.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;

namespace SC880
{
    public sealed class ConstantItem
    {
        public string Name;
        public string Symbol;
        public double Value;
        public string Unit;

        public ConstantItem(string name, string symbol, double value, string unit)
        {
            Name = name;
            Symbol = symbol;
            Value = value;
            Unit = unit;
        }

        public string ValueText
        {
            get { return Value.ToString("G17", CultureInfo.InvariantCulture); }
        }
    }

    public static class ConstantsData
    {
        public static readonly List<ConstantItem> Items = new List<ConstantItem>
        {
            new ConstantItem("Скорость света в вакууме", "c", 299792458.0, "м/с"),
            new ConstantItem("Постоянная Планка", "h", 6.62607015e-34, "Дж·с"),
            new ConstantItem("Приведённая постоянная Планка", "ħ", 1.054571817e-34, "Дж·с"),
            new ConstantItem("Гравитационная постоянная", "G", 6.67430e-11, "м³/(кг·с²)"),
            new ConstantItem("Ускорение свободного падения", "g", 9.80665, "м/с²"),
            new ConstantItem("Элементарный заряд", "e", 1.602176634e-19, "Кл"),
            new ConstantItem("Масса электрона", "me", 9.1093837015e-31, "кг"),
            new ConstantItem("Масса протона", "mp", 1.67262192369e-27, "кг"),
            new ConstantItem("Масса нейтрона", "mn", 1.67492749804e-27, "кг"),
            new ConstantItem("Постоянная Авогадро", "NA", 6.02214076e23, "1/моль"),
            new ConstantItem("Постоянная Больцмана", "k", 1.380649e-23, "Дж/К"),
            new ConstantItem("Универсальная газовая постоянная", "R", 8.314462618, "Дж/(моль·К)"),
            new ConstantItem("Постоянная Фарадея", "F", 96485.33212, "Кл/моль"),
            new ConstantItem("Электрическая постоянная", "ε0", 8.8541878128e-12, "Ф/м"),
            new ConstantItem("Магнитная постоянная", "μ0", 1.25663706212e-6, "Гн/м"),
            new ConstantItem("Постоянная Кулона", "kе", 8.9875517923e9, "Н·м²/Кл²"),
            new ConstantItem("Атомная единица массы", "u", 1.66053906660e-27, "кг"),
            new ConstantItem("Радиус Бора", "a0", 5.29177210903e-11, "м"),
            new ConstantItem("Магнетон Бора", "μB", 9.2740100783e-24, "Дж/Тл"),
            new ConstantItem("Ядерный магнетон", "μN", 5.0507837461e-27, "Дж/Тл"),
            new ConstantItem("Постоянная Ридберга", "R∞", 10973731.568160, "1/м"),
            new ConstantItem("Постоянная Стефана–Больцмана", "σ", 5.670374419e-8, "Вт/(м²·К⁴)"),
            new ConstantItem("Постоянная Вина", "b", 2.897771955e-3, "м·К"),
            new ConstantItem("Молярный объём газа", "Vm", 0.02241396954, "м³/моль"),
            new ConstantItem("Электрон-вольт", "eV", 1.602176634e-19, "Дж"),
            new ConstantItem("Астрономическая единица", "AU", 1.495978707e11, "м"),
            new ConstantItem("Световой год", "ly", 9.4607304725808e15, "м"),
            new ConstantItem("Парсек", "pc", 3.08567758149137e16, "м"),
            new ConstantItem("Масса Земли", "M⊕", 5.9722e24, "кг"),
            new ConstantItem("Радиус Земли", "R⊕", 6.371e6, "м"),
            new ConstantItem("Масса Солнца", "M☉", 1.98847e30, "кг"),
            new ConstantItem("Радиус Солнца", "R☉", 6.9634e8, "м"),
            new ConstantItem("Стандартная атмосфера", "atm", 101325.0, "Па"),
            new ConstantItem("Ноль Цельсия", "0°C", 273.15, "К"),
            new ConstantItem("Магнитный поток (квант)", "Φ0", 2.067833848e-15, "Вб"),
            new ConstantItem("Квант проводимости", "G0", 7.748091729e-5, "См"),
            new ConstantItem("Классический радиус электрона", "re", 2.8179403262e-15, "м"),
            new ConstantItem("Комптоновская длина волны", "λC", 2.42631023867e-12, "м"),
            new ConstantItem("Постоянная тонкой структуры", "α", 7.2973525693e-3, ""),
            new ConstantItem("Энергия Хартри", "Eh", 4.3597447222071e-18, "Дж"),
            new ConstantItem("Атмосферное давление (мм рт. ст.)", "mmHg", 133.322387415, "Па"),
            new ConstantItem("Число Лошмидта", "n0", 2.6867811e25, "1/м³")
        };
    }

    public sealed class UnitItem
    {
        public string Name;
        public double Factor;

        public UnitItem(string name, double factor)
        {
            Name = name;
            Factor = factor;
        }
    }

    public sealed class UnitCategory
    {
        public string Name;
        public bool Temperature;
        public List<UnitItem> Units = new List<UnitItem>();

        public UnitCategory(string name, bool temperature)
        {
            Name = name;
            Temperature = temperature;
        }

        public UnitCategory Add(string name, double factor)
        {
            Units.Add(new UnitItem(name, factor));
            return this;
        }
    }

    public static class UnitsData
    {
        public static readonly List<UnitCategory> Categories = Build();

        private static List<UnitCategory> Build()
        {
            List<UnitCategory> list = new List<UnitCategory>();

            UnitCategory length = new UnitCategory("Длина", false);
            length.Add("метр (м)", 1);
            length.Add("километр (км)", 1000);
            length.Add("сантиметр (см)", 0.01);
            length.Add("миллиметр (мм)", 0.001);
            length.Add("микрометр (мкм)", 1e-6);
            length.Add("нанометр (нм)", 1e-9);
            length.Add("ангстрем (Å)", 1e-10);
            length.Add("дюйм (in)", 0.0254);
            length.Add("фут (ft)", 0.3048);
            length.Add("ярд (yd)", 0.9144);
            length.Add("миля (mi)", 1609.344);
            length.Add("морская миля", 1852);
            length.Add("астрономическая единица", 1.495978707e11);
            length.Add("световой год", 9.4607304725808e15);
            list.Add(length);

            UnitCategory area = new UnitCategory("Площадь", false);
            area.Add("квадратный метр (м²)", 1);
            area.Add("квадратный километр (км²)", 1e6);
            area.Add("квадратный сантиметр (см²)", 1e-4);
            area.Add("квадратный миллиметр (мм²)", 1e-6);
            area.Add("гектар (га)", 1e4);
            area.Add("ар (сотка)", 100);
            area.Add("акр", 4046.8564224);
            area.Add("квадратный дюйм (in²)", 0.00064516);
            area.Add("квадратный фут (ft²)", 0.09290304);
            area.Add("квадратная миля (mi²)", 2589988.110336);
            list.Add(area);

            UnitCategory volume = new UnitCategory("Объём", false);
            volume.Add("кубический метр (м³)", 1);
            volume.Add("литр (л)", 0.001);
            volume.Add("миллилитр (мл)", 1e-6);
            volume.Add("кубический сантиметр (см³)", 1e-6);
            volume.Add("кубический миллиметр (мм³)", 1e-9);
            volume.Add("галлон США", 0.003785411784);
            volume.Add("галлон английский", 0.00454609);
            volume.Add("пинта США", 0.000473176473);
            volume.Add("баррель нефтяной", 0.158987294928);
            volume.Add("кубический фут (ft³)", 0.028316846592);
            volume.Add("кубический дюйм (in³)", 1.6387064e-5);
            list.Add(volume);

            UnitCategory mass = new UnitCategory("Масса", false);
            mass.Add("килограмм (кг)", 1);
            mass.Add("грамм (г)", 0.001);
            mass.Add("миллиграмм (мг)", 1e-6);
            mass.Add("тонна (т)", 1000);
            mass.Add("фунт (lb)", 0.45359237);
            mass.Add("унция (oz)", 0.028349523125);
            mass.Add("карат", 0.0002);
            mass.Add("тонна США (short ton)", 907.18474);
            mass.Add("тонна английская (long ton)", 1016.0469088);
            mass.Add("центнер", 100);
            list.Add(mass);

            UnitCategory temp = new UnitCategory("Температура", true);
            temp.Add("градус Цельсия (°C)", 1);
            temp.Add("градус Фаренгейта (°F)", 1);
            temp.Add("кельвин (K)", 1);
            temp.Add("градус Ранкина (°R)", 1);
            list.Add(temp);

            UnitCategory speed = new UnitCategory("Скорость", false);
            speed.Add("метр в секунду (м/с)", 1);
            speed.Add("километр в час (км/ч)", 1.0 / 3.6);
            speed.Add("миля в час (mph)", 0.44704);
            speed.Add("узел (kn)", 0.5144444444444444);
            speed.Add("фут в секунду (ft/s)", 0.3048);
            speed.Add("скорость звука (Мах)", 340.29);
            list.Add(speed);

            UnitCategory time = new UnitCategory("Время", false);
            time.Add("секунда (с)", 1);
            time.Add("миллисекунда (мс)", 0.001);
            time.Add("минута (мин)", 60);
            time.Add("час (ч)", 3600);
            time.Add("сутки", 86400);
            time.Add("неделя", 604800);
            time.Add("год (365.25 сут)", 31557600);
            list.Add(time);

            UnitCategory energy = new UnitCategory("Энергия", false);
            energy.Add("джоуль (Дж)", 1);
            energy.Add("килоджоуль (кДж)", 1000);
            energy.Add("калория (кал)", 4.184);
            energy.Add("килокалория (ккал)", 4184);
            energy.Add("ватт-час (Вт·ч)", 3600);
            energy.Add("киловатт-час (кВт·ч)", 3.6e6);
            energy.Add("электрон-вольт (эВ)", 1.602176634e-19);
            energy.Add("BTU", 1055.05585262);
            energy.Add("фут-фунт", 1.35581794833);
            list.Add(energy);

            UnitCategory pressure = new UnitCategory("Давление", false);
            pressure.Add("паскаль (Па)", 1);
            pressure.Add("килопаскаль (кПа)", 1000);
            pressure.Add("мегапаскаль (МПа)", 1e6);
            pressure.Add("бар", 1e5);
            pressure.Add("атмосфера (атм)", 101325);
            pressure.Add("мм ртутного столба", 133.322387415);
            pressure.Add("psi", 6894.757293168);
            pressure.Add("кгс/см²", 98066.5);
            list.Add(pressure);

            UnitCategory power = new UnitCategory("Мощность", false);
            power.Add("ватт (Вт)", 1);
            power.Add("киловатт (кВт)", 1000);
            power.Add("лошадиная сила (л.с.)", 735.49875);
            power.Add("horsepower (hp)", 745.699871582);
            power.Add("калория в секунду", 4.184);
            power.Add("BTU в час", 0.293071070172);
            list.Add(power);

            UnitCategory force = new UnitCategory("Сила", false);
            force.Add("ньютон (Н)", 1);
            force.Add("килоньютон (кН)", 1000);
            force.Add("килограмм-сила (кгс)", 9.80665);
            force.Add("фунт-сила (lbf)", 4.4482216152605);
            force.Add("дина (дин)", 1e-5);
            list.Add(force);

            UnitCategory angle = new UnitCategory("Угол", false);
            angle.Add("градус (°)", 1);
            angle.Add("радиан (рад)", 57.29577951308232);
            angle.Add("градиан (град)", 0.9);
            angle.Add("минута (′)", 1.0 / 60);
            angle.Add("секунда (″)", 1.0 / 3600);
            angle.Add("оборот", 360);
            list.Add(angle);

            return list;
        }

        public static double Convert(UnitCategory category, int from, int to, double value)
        {
            if (from < 0 || from >= category.Units.Count || to < 0 || to >= category.Units.Count)
                throw new CalcError("Единица не выбрана");
            if (category.Temperature)
            {
                double celsius;
                switch (from)
                {
                    case 0: celsius = value; break;
                    case 1: celsius = (value - 32) * 5.0 / 9.0; break;
                    case 2: celsius = value - 273.15; break;
                    default: celsius = (value - 491.67) * 5.0 / 9.0; break;
                }
                switch (to)
                {
                    case 0: return celsius;
                    case 1: return celsius * 9.0 / 5.0 + 32;
                    case 2: return celsius + 273.15;
                    default: return (celsius + 273.15) * 9.0 / 5.0;
                }
            }
            return value * category.Units[from].Factor / category.Units[to].Factor;
        }
    }
}

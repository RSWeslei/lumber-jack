using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LumberJack.Editor
{
    /// <summary>
    /// Calculadora simples no Editor (Editor Windows > Calculator).
    /// </summary>
    public class Calculator : EditorWindow
    {
        private string _display = "0";
        private float _windowWidth;
        private float _windowHeight;
        private float _dividedWidth;
        private float _dividedHeight;
        private readonly List<char> _digits = new List<char>(11) { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' };
        private readonly List<char> _operators = new List<char>(4) { '+', '-', '×', '÷' };
        private bool _enteredOperator = false;
        private bool _enteredDot = false;
        private bool _resetUponDigitInput = false;

        [MenuItem("Editor Windows/Calculator")]
        public static void ShowWindow()
        {
            GetWindow(typeof(Calculator));
        }

        private void OnGUI()
        {
            _windowWidth = position.width;
            _windowHeight = position.height;
            _dividedWidth = _windowWidth / 4;
            _dividedHeight = _windowHeight / 7;


            GUI.Box(new Rect(0, 0, _windowWidth, _dividedHeight), _display);

            if (GUI.Button(new Rect(0, _dividedHeight * 1, _dividedWidth, _dividedHeight), "π")) {
                EnterPI();
            }
            if (GUI.Button(new Rect(0, _dividedHeight * 2, _dividedWidth, _dividedHeight), "1/x")) {
                CalculateOver1();
            }
            if (GUI.Button(new Rect(0, _dividedHeight * 3, _dividedWidth, _dividedHeight), "7")) {
                DigitPushed("7");
            }
            if (GUI.Button(new Rect(0, _dividedHeight * 4, _dividedWidth, _dividedHeight), "4")) {
                DigitPushed("4");
            }
            if (GUI.Button(new Rect(0, _dividedHeight * 5, _dividedWidth, _dividedHeight), "1")) {
                DigitPushed("1");
            }
            if (GUI.Button(new Rect(0, _dividedHeight * 6, _dividedWidth, _dividedHeight), "0")) {
                DigitPushed("0");
            }
            if (GUI.Button(new Rect(_dividedWidth, _dividedHeight * 1, _dividedWidth, _dividedHeight), "CE")) {
                ClearCurrentEntry();
            }
            if (GUI.Button(new Rect(_dividedWidth, _dividedHeight * 2, _dividedWidth, _dividedHeight), "x²")) {
                CalculateToThePowerOfTwo();
            }
            if (GUI.Button(new Rect(_dividedWidth, _dividedHeight * 3, _dividedWidth, _dividedHeight), "8")) {
                DigitPushed("8");
            }
            if (GUI.Button(new Rect(_dividedWidth, _dividedHeight * 4, _dividedWidth, _dividedHeight), "5")) {
                DigitPushed("5");
            }
            if (GUI.Button(new Rect(_dividedWidth, _dividedHeight * 5, _dividedWidth, _dividedHeight), "2")) {
                DigitPushed("2");
            }
            if (GUI.Button(new Rect(_dividedWidth, _dividedHeight * 6, _dividedWidth, _dividedHeight), ".")) {
                DigitPushed(".");
            }
            if (GUI.Button(new Rect(_dividedWidth * 2, _dividedHeight * 1, _dividedWidth, _dividedHeight), "C")) {
                ClearAllEntries();
            }
            if (GUI.Button(new Rect(_dividedWidth * 2, _dividedHeight * 2, _dividedWidth, _dividedHeight), "x³")) {
                CalculateToThePowerOfThree();
            }
            if (GUI.Button(new Rect(_dividedWidth * 2, _dividedHeight * 3, _dividedWidth, _dividedHeight), "9")) {
                DigitPushed("9");
            }
            if (GUI.Button(new Rect(_dividedWidth * 2, _dividedHeight * 4, _dividedWidth, _dividedHeight), "6")) {
                DigitPushed("6");
            }
            if (GUI.Button(new Rect(_dividedWidth * 2, _dividedHeight * 5, _dividedWidth, _dividedHeight), "3")) {
                DigitPushed("3");
            }
            if (GUI.Button(new Rect(_dividedWidth * 2, _dividedHeight * 6, _dividedWidth, _dividedHeight), "=")) {
                CalculateAnswer();
            }
            if (GUI.Button(new Rect(_dividedWidth * 3, _dividedHeight * 1, _dividedWidth, _dividedHeight), "←")) {
                RemoveDigit();
            }
            if (GUI.Button(new Rect(_dividedWidth * 3, _dividedHeight * 2, _dividedWidth, _dividedHeight), "√x")) {
                CalculateTheSquareRoot();
            }
            if (GUI.Button(new Rect(_dividedWidth * 3, _dividedHeight * 3, _dividedWidth, _dividedHeight), "÷")) {
                OperatorPushed("÷");
            }
            if (GUI.Button(new Rect(_dividedWidth * 3, _dividedHeight * 4, _dividedWidth, _dividedHeight), "×")) {
                OperatorPushed("×");
            }
            if (GUI.Button(new Rect(_dividedWidth * 3, _dividedHeight * 5, _dividedWidth, _dividedHeight), "-")) {
                OperatorPushed("-");
            }
            if (GUI.Button(new Rect(_dividedWidth * 3, _dividedHeight * 6, _dividedWidth, _dividedHeight), "+")) {
                OperatorPushed("+");
            }
        }

        private void DigitPushed(string digit)
        {
            if (_resetUponDigitInput)
            {
                _resetUponDigitInput = false;
                _display = "0";
            }
            if (_enteredDot == true && digit == ".")
                return;
            if (digit == ".")
                _enteredDot = true;
            if (_display == "0" && digit != ".")
                _display = _display.Substring(0, _display.Length - 1);
            _display += digit;
            _enteredOperator = false;
        }

        private void OperatorPushed(string operation)
        {
            if (_enteredOperator == true)
                return;
            _display += operation;
            _enteredOperator = true;
            _enteredDot = false;
            _resetUponDigitInput = false;
        }

        private void RemoveDigit()
        {
            _resetUponDigitInput = false;
            if (_display.Length - 1 <= 0)
            {
                _display = "0";
                return;
            }
            if (_display[_display.Length - 1] == '.')
                _enteredDot = false;
            _display = _display.Substring(0, _display.Length - 1);
        }

        private void CalculateAnswer()
        {
            string entry = string.Empty;
            int expressionLength = _display.Length;
            List<float> expressionEntries = new List<float>();
            List<float> expressionOperators = new List<float>();
            int mdNumber = 0;

            for (int x = 0; x < expressionLength; x++)
            {
                if (_digits.Contains(_display[0]))
                {
                    entry += _display[0].ToString();
                    if (_display.Length == 1)
                        expressionEntries.Add(float.Parse(entry, System.Globalization.CultureInfo.InvariantCulture));
                }
                else if (_operators.Contains(_display[0]))
                {
                    if (_display[0] == '×' || _display[0] == '÷')
                        mdNumber++;
                    expressionEntries.Add(float.Parse(entry, System.Globalization.CultureInfo.InvariantCulture));
                    expressionOperators.Add(_display[0]);
                    entry = string.Empty;
                }
                _display = _display.Substring(1);
            }

            for (int y = 0; y < mdNumber; y++)
            {
                for (int w = 0; w < Mathf.Infinity; w++)
                {
                    float orderOfOperationResult;
                    if (expressionOperators[w] == '×')
                    {
                        orderOfOperationResult = expressionEntries[w] * expressionEntries[w + 1];
                        expressionEntries[w] = orderOfOperationResult;
                        expressionEntries.Remove(expressionEntries[w + 1]);
                        expressionOperators.Remove(expressionOperators[w]);
                        break;
                    }
                    else if (expressionOperators[w] == '÷')
                    {
                        orderOfOperationResult = expressionEntries[w] / expressionEntries[w + 1];
                        expressionEntries[w] = orderOfOperationResult;
                        expressionEntries.Remove(expressionEntries[w + 1]);
                        expressionOperators.Remove(expressionOperators[w]);
                        break;
                    }
                }
            }

            float result = expressionEntries[0];
            for (int z = 0; z < expressionOperators.Count; z++)
            {
                if (expressionOperators[z] == '+')
                    result += expressionEntries[z + 1];
                else if (expressionOperators[z] == '-')
                    result -= expressionEntries[z + 1];
            }

            _display = result.ToString();
            _resetUponDigitInput = true;
        }

        private void ClearAllEntries() // C
        {
            _display = "0";
            _enteredOperator = false;
            _enteredDot = false;
            _resetUponDigitInput = false;
        }

        private void ClearCurrentEntry() // CE
        {
            for (int x = 0; x < Mathf.Infinity; x++)
            {
                if (_operators.Contains(_display[_display.Length - 1]))
                    break;
                else if (_display.Length - 1 == 0)
                {
                    _display = "0";
                    break;
                }
                else if (_digits.Contains(_display[_display.Length - 1]))
                    _display = _display.Substring(0, _display.Length - 1);
            }
            _enteredOperator = false;
            _enteredDot = false;
            _resetUponDigitInput = false;
        }

        private float ReturnEndEntry() // find and return the entry at the end
        {
            string entryString = string.Empty;
            for (int x = 0; x < _display.Length; x++)
            {
                entryString += _display[x];
                if (_operators.Contains(_display[x]))
                    entryString = string.Empty;
            }
            float entry = float.Parse(entryString, System.Globalization.CultureInfo.InvariantCulture);
            return entry;
        }

        private void ChangeEndEntry(float calculation) // apply the calculation
        {
            if (_display == "0")
            {
                _display = calculation.ToString();
                return;
            }
            _display += calculation.ToString();
        }

        private void EnterPI()
        {
            ClearCurrentEntry();
            if (_display == "0")
                _display = string.Empty;
            _display += "3.141593";
            _resetUponDigitInput = true;
        }

        private void CalculateTheSquareRoot()
        {
            float calculation = Mathf.Sqrt(ReturnEndEntry());
            ClearCurrentEntry();
            ChangeEndEntry(calculation);
            _resetUponDigitInput = true;
        }

        private void CalculateToThePowerOfTwo()
        {
            float calculation = Mathf.Pow(ReturnEndEntry(), 2);
            ClearCurrentEntry();
            ChangeEndEntry(calculation);
            _resetUponDigitInput = true;
        }

        private void CalculateToThePowerOfThree()
        {
            float calculation = Mathf.Pow(ReturnEndEntry(), 3);
            ClearCurrentEntry();
            ChangeEndEntry(calculation);
            _resetUponDigitInput = true;
        }

        private void CalculateOver1()
        {
            float calculation = (1 / ReturnEndEntry());
            ClearCurrentEntry();
            ChangeEndEntry(calculation);
            _resetUponDigitInput = true;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Calculator : EditorWindow
{
    string display = "0";
    float windowWidth;
    float windowHeight;
    float dividedWidth;
    float dividedHeight;
    List<char> digits = new List<char>(11) { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' };
    List<char> operators = new List<char>(4) { '+', '-', '×', '÷' };
    bool enteredOperator = false;
    bool enteredDot = false;
    bool resetUponDigitInput = false;

    [MenuItem("Editor Windows/Calculator")]
    public static void ShowWindow()
    {
        GetWindow(typeof(Calculator));
    }

    private void OnGUI()
    {
        windowWidth = position.width;
        windowHeight = position.height;
        dividedWidth = windowWidth / 4;
        dividedHeight = windowHeight / 7;
        

        GUI.Box(new Rect(0, 0, windowWidth, dividedHeight), display);

        if (GUI.Button(new Rect(0, dividedHeight * 1, dividedWidth, dividedHeight), "?")) {
            EnterPI();
        }
        if (GUI.Button(new Rect(0, dividedHeight * 2, dividedWidth, dividedHeight), "1/x")) {
            CalculateOver1();
        }
        if (GUI.Button(new Rect(0, dividedHeight * 3, dividedWidth, dividedHeight), "7")) {
            DigitPushed("7");
        }
        if (GUI.Button(new Rect(0, dividedHeight * 4, dividedWidth, dividedHeight), "4")) {
            DigitPushed("4");
        }
        if (GUI.Button(new Rect(0, dividedHeight * 5, dividedWidth, dividedHeight), "1")) {
            DigitPushed("1");
        }
        if (GUI.Button(new Rect(0, dividedHeight * 6, dividedWidth, dividedHeight), "0")) {
            DigitPushed("0");
        }
        if (GUI.Button(new Rect(dividedWidth, dividedHeight * 1, dividedWidth, dividedHeight), "CE")) {
            ClearCurrentEntry();
        }
        if (GUI.Button(new Rect(dividedWidth, dividedHeight * 2, dividedWidth, dividedHeight), "x²")) {
            CalculateToThePowerOfTwo();
        }
        if (GUI.Button(new Rect(dividedWidth, dividedHeight * 3, dividedWidth, dividedHeight), "8")) {
            DigitPushed("8");
        }
        if (GUI.Button(new Rect(dividedWidth, dividedHeight * 4, dividedWidth, dividedHeight), "5")) {
            DigitPushed("5");
        }
        if (GUI.Button(new Rect(dividedWidth, dividedHeight * 5, dividedWidth, dividedHeight), "2")) {
            DigitPushed("2");
        }
        if (GUI.Button(new Rect(dividedWidth, dividedHeight * 6, dividedWidth, dividedHeight), ".")) {
            DigitPushed(".");
        }
        if (GUI.Button(new Rect(dividedWidth * 2, dividedHeight * 1, dividedWidth, dividedHeight), "C")) {
            ClearAllEntries();
        }
        if (GUI.Button(new Rect(dividedWidth * 2, dividedHeight * 2, dividedWidth, dividedHeight), "x³")) {
            CalculateToThePowerOfThree();
        }
        if (GUI.Button(new Rect(dividedWidth * 2, dividedHeight * 3, dividedWidth, dividedHeight), "9")) {
            DigitPushed("9");
        }
        if (GUI.Button(new Rect(dividedWidth * 2, dividedHeight * 4, dividedWidth, dividedHeight), "6")) {
            DigitPushed("6");
        }
        if (GUI.Button(new Rect(dividedWidth * 2, dividedHeight * 5, dividedWidth, dividedHeight), "3")) {
            DigitPushed("3");
        }
        if (GUI.Button(new Rect(dividedWidth * 2, dividedHeight * 6, dividedWidth, dividedHeight), "=")) {
            CalculateAnswer();
        }
        if (GUI.Button(new Rect(dividedWidth * 3, dividedHeight * 1, dividedWidth, dividedHeight), "?")) {
            RemoveDigit();
        }
        if (GUI.Button(new Rect(dividedWidth * 3, dividedHeight * 2, dividedWidth, dividedHeight), "²?x")) {
            CalculateTheSquareRoot();
        }
        if (GUI.Button(new Rect(dividedWidth * 3, dividedHeight * 3, dividedWidth, dividedHeight), "÷")) {
            OperatorPushed("÷");
        }
        if (GUI.Button(new Rect(dividedWidth * 3, dividedHeight * 4, dividedWidth, dividedHeight), "×")) {
            OperatorPushed("×");
        }
        if (GUI.Button(new Rect(dividedWidth * 3, dividedHeight * 5, dividedWidth, dividedHeight), "-")) {
            OperatorPushed("-");
        }
        if (GUI.Button(new Rect(dividedWidth * 3, dividedHeight * 6, dividedWidth, dividedHeight), "+")) {
            OperatorPushed("+");
        }
    }

    private void DigitPushed(string digit)
    {
        if (resetUponDigitInput)
        {
            resetUponDigitInput = false;
            display = "0";
        }
        if (enteredDot == true && digit == ".")
            return;
        if (digit == ".")
            enteredDot = true;
        if (display == "0" && digit != ".")
            display = display.Substring(0, display.Length - 1);
        display += digit;
        enteredOperator = false;
    }

    private void OperatorPushed(string operation)
    {
        if (enteredOperator == true)
            return;
        display += operation;
        enteredOperator = true;
        enteredDot = false;
        resetUponDigitInput = false;
    }

    private void RemoveDigit()
    {
        resetUponDigitInput = false;
        if (display.Length - 1 <= 0)
        {
            display = "0";
            return;
        }
        if (display[display.Length - 1] == '.')
            enteredDot = false;
        display = display.Substring(0, display.Length - 1);
    }

    private void CalculateAnswer()
    {
        string entry = string.Empty;
        int expressionLength = display.Length;
        List<float> expressionEntries = new List<float>();
        List<float> expressionOperators = new List<float>();
        int mdNumber = 0;

        for (int x = 0; x < expressionLength; x++)
        {
            if (digits.Contains(display[0]))
            {
                entry += display[0].ToString();
                if (display.Length == 1)
                    expressionEntries.Add(float.Parse(entry, System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (operators.Contains(display[0]))
            {
                if (display[0] == '×' || display[0] == '÷')
                    mdNumber++;
                expressionEntries.Add(float.Parse(entry, System.Globalization.CultureInfo.InvariantCulture));
                expressionOperators.Add(display[0]);
                entry = string.Empty;
            }
            display = display.Substring(1);
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

        display = result.ToString();
        resetUponDigitInput = true;
    }

    private void ClearAllEntries() // C
    {
        display = "0";
        enteredOperator = false;
        enteredDot = false;
        resetUponDigitInput = false;
    }

    private void ClearCurrentEntry() // CE
    {
        for (int x = 0; x < Mathf.Infinity; x++)
        {
            if (operators.Contains(display[display.Length - 1]))
                break;
            else if (display.Length - 1 == 0)
            {
                display = "0";
                break;
            }
            else if (digits.Contains(display[display.Length - 1]))
                display = display.Substring(0, display.Length - 1);
        }
        enteredOperator = false;
        enteredDot = false;
        resetUponDigitInput = false;
    }

    private float ReturnEndEntry() // find and return the entry at the end
    {
        string entryString = string.Empty;
        for (int x = 0; x < display.Length; x++)
        {
            entryString += display[x];
            if (operators.Contains(display[x]))
                entryString = string.Empty;
        }
        float entry = float.Parse(entryString, System.Globalization.CultureInfo.InvariantCulture);
        return entry;
    }

    private void ChangeEndEntry(float calculation) // apply the calculation
    {
        if (display == "0")
        {
            display = calculation.ToString();
            return;
        }
        display += calculation.ToString();
    }

    private void EnterPI()
    {
        ClearCurrentEntry();
        if (display == "0")
            display = string.Empty;
        display += "3.141593";
        resetUponDigitInput = true;
    }

    private void CalculateTheSquareRoot()
    {
        float calculation = Mathf.Sqrt(ReturnEndEntry());
        ClearCurrentEntry();
        ChangeEndEntry(calculation);
        resetUponDigitInput = true;
    }

    private void CalculateToThePowerOfTwo()
    {
        float calculation = Mathf.Pow(ReturnEndEntry(), 2);
        ClearCurrentEntry();
        ChangeEndEntry(calculation);
        resetUponDigitInput = true;
    }

    private void CalculateToThePowerOfThree()
    {
        float calculation = Mathf.Pow(ReturnEndEntry(), 3);
        ClearCurrentEntry();
        ChangeEndEntry(calculation);
        resetUponDigitInput = true;
    }

    private void CalculateOver1()
    {
        float calculation = (1 / ReturnEndEntry());
        ClearCurrentEntry();
        ChangeEndEntry(calculation);
        resetUponDigitInput = true;
    }
}
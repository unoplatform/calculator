// GraphControl shim — enums transcribed verbatim from microsoft/calculator @ 4fd3fc53:
//   src/GraphControl/Models/Equation.h  and  src/GraphControl/Control/Grapher.h
//
// The numeric values are load-bearing. EquationViewModel.EquationErrorText switches over
// 19 EvaluationErrorCode and 27 SyntaxErrorCode members; two members sharing a value is CS0152.
#nullable enable

namespace GraphControl;

public enum EquationLineStyle
{
    Solid,
    Dot,
    Dash,
    DashDot,
    DashDotDot
}

public enum ErrorType
{
    Evaluation,
    Syntax,
    Abort
}

public enum GraphViewChangedReason
{
    Manipulation,
    Reset
}

public enum EvaluationErrorCode
{
    Overflow = 2,
    RequireRadiansMode = 3,
    TooComplexToSolve = 4,
    RequireDegreesMode = 5,
    FactorialInvalidArgument = -1,
    Factorial2InvalidArgument = -2,
    FactorialCannotPerformOnLargeNumber = -3,
    ModuloCannotPerformOnFloat = -5,
    EquationTooComplexToSolveSymbolic = -7,
    EquationHasNoSolution = -8,
    EquationTooComplexToSolve = -9,
    EquationTooComplexToPlot = -10,
    DivideByZero = -15,
    InequalityTooComplexToSolve = -41,
    InequalityHasNoSolution = -42,
    MutuallyExclusiveConditions = -43,
    OutOfDomain = -101,
    GE_NotSupported = -503,
    GE_GeneralError = -504,
    GE_TooComplexToSolve = -506
}

public enum SyntaxErrorCode
{
    ParenthesisMismatch = 1,
    UnmatchedParenthesis = 2,
    TooManyDecimalPoints = 3,
    DecimalPointWithoutDigits = 4,
    UnexpectedEndOfExpression = 5,
    UnexpectedToken = 6,
    InvalidToken = 7,
    TooManyEquals = 8,
    EqualWithoutGraphVariable = 10,
    InvalidEquationSyntax = 11,
    EmptyExpression = 12,
    EqualWithoutEquation = 14,
    InvalidEquationFormat = 15,
    ExpectParenthesisAfterFunctionName = 25,
    IncorrectNumParameter = 26,
    InvalidVariableNameFormat = 32,
    BracketMismatch = 34,
    UnmatchedBracket = 35,
    InvalidMathMLFormat = 40,
    UnknownMathMLEntity = 41,
    UnknownMathMLElement = 42,
    CannotUseIInReal = 48,
    GeneralError = 52,
    InvalidNumberDigit = 55,
    InvalidNumberBase = 56,
    InvalidVariableSpecification = 57,
    ExpectingLogicalOperands = 58,
    ExpectingScalarOperands = 59,
    CannotMixLogicalScalarInList = 60,
    CannotUseIndexVarInOpLimits = 61,
    CannotUseIndexVarInLimPoint = 62,
    CannotUseComplexInfinityInReal = 72,
    CannotUseIInInequalitySolving = 123,
    RichEditSerializationError = 201,
    RichEditInitialization = 202,
    RichEditInlineObjectStructure = 203,
    RichEditMissingArgument = 204,
    RichEditGeneralError = 210
}

grammar Phonix;

/* Code generation rules */

options
{
    language=CSharp2;
}

tokens
{
    PLUS = '+';
    MINUS = '-';
    NULL = '*';
    EQ = '=';
    LBRACE = '[';
    RBRACE = ']';
    LPAREN = '(';
    RPAREN = ')';
    IMPORT = 'import';
    FEATURE = 'feature';
    SYMBOL = 'symbol';
    RULE = 'rule';
    SLASH = '/';
    POUND = '#';
    ARROW = '=>';
    UNDERSCORE = '_';
    BOUNDARY = '$';
}

@header
{
    using Phonix;
    using System.Collections.Generic;
}

@parser::namespace { Phonix.Parse }
@lexer::namespace { Phonix.Parse }

@members
{
    private Phonology _phono;
    private string _currentFile;
    private bool _parseError = false;
}

@rulecatch
{
catch (RecognitionException re) 
{
    _parseError = true;
    Console.Error.WriteLine(String.Format(
        "couldn't parse {0} at {1} line {2} char {3}", 
        GetTokenErrorDisplay(re.Token), 
        _currentFile, 
        re.Line,
        re.CharPositionInLine)
    );
    Recover(input,re);
}
catch (PhonixException px)
{
    IToken token = input.LT(-1);
    Console.Error.WriteLine(String.Format("{0} at {1} line {2}", px.Message, _currentFile, token.Line));
    _parseError = true;
}
catch (ArgumentNullException nx)
{
    if (_parseError)
    {
        // after a parsing error, all bets are off, so we swallow this exception
    }
    else
    {
        throw;
    }
}
}

/* Lexer Rules */

fragment DIGIT: '0'..'9';
fragment ALPHA: 'a'..'z' | 'A'..'Z';
fragment SPACE: ' ' | '\t';
fragment EOL: '\r' | '\n';
fragment RESERVED_START: '+' | '-' | '*' | '=' | '[' | ']' | '(' | ')' | '/' | '#' | '_' | '$' | '"' | '\'';
fragment RESERVED_MID: '[' | ']' | '(' | ')' | '=' | '$';
fragment CHAR_START: ~( SPACE | EOL | RESERVED_START );
fragment CHAR_MID: ~( SPACE | EOL | RESERVED_MID );
fragment SQ: '\'';
fragment DQ: '"';
NUMBER: (DIGIT)+;
LABEL: (ALPHA | DIGIT)+;
STR: CHAR_START (CHAR_MID)*;
QSTR: ( SQ ( ~SQ )* SQ ) | ( DQ ( ~DQ )* DQ );
COMMENT: POUND ~EOL* EOL { Skip(); };
WS: (SPACE | EOL)+ { Skip(); };

/* String rules */

str returns [string text]:
        STR { $text = $STR.text; }
    |   QSTR { $text = $QSTR.text.Substring(1, $QSTR.text.Length - 2); }
    |   LABEL { $text = $LABEL.text; }
    |   NUMBER { $text = $NUMBER.text; }
    ;

label returns [string text]:
        LABEL { $text = $LABEL.text; }
    |   NUMBER { $text = $NUMBER.text; }
    ;

/* Parser Rules */

parseRoot[string currentFile, Phonology phono]
    @init { _phono = $phono; _currentFile = $currentFile; }:
    phonixDecl* EOF
    { if (_parseError) throw new ParseException(currentFile); }
    ;

phonixDecl:
        importDecl
    |   featureDecl { _phono.FeatureSet.Add($featureDecl.f); }
    |   symbolDecl { _phono.SymbolSet.Add($symbolDecl.s); }
    |   ruleDecl { _phono.RuleSet.Add($ruleDecl.r); }
    ;

/* File import */

importDecl:
    IMPORT str
    { Util.ParseFile(_phono, _currentFile, $str.text); }
    ;

/* Feature declarations */

featureDecl returns [Feature f]: 
    FEATURE str paramList?
    { $f = Util.MakeFeature($str.text, $paramList.list, _phono); }
    ;

feature returns [Feature f]:
    str
    { $f = _phono.FeatureSet.Get<Feature>($str.text); }
    ;

unaryFeature returns [UnaryFeature f]:
    str
    { $f = _phono.FeatureSet.Get<UnaryFeature>($str.text); }
    ;

binaryFeature returns [BinaryFeature f]:
    str
    { $f = _phono.FeatureSet.Get<BinaryFeature>($str.text); }
    ;

scalarFeature returns [ScalarFeature f]:
    str
    { $f = _phono.FeatureSet.Get<ScalarFeature>($str.text); }
    ;

nodeFeature returns [NodeFeature f]:
    str
    { $f = _phono.FeatureSet.Get<NodeFeature>($str.text); }
    ;

/* Symbol declarations and usage */

symbolDecl returns [Symbol s]: 
    SYMBOL str paramList? matrix
    { $s = new Symbol($str.text, $matrix.fm); }
    ;

symbolStr returns [List<Symbol> slist]
    @init { $slist = new List<Symbol>(); }:
    str
    { $slist = _phono.SymbolSet.SplitSymbols($str.text); }
    ;

/* Rule declarations */

ruleDecl returns [Rule r]: 
    RULE str paramList? rule
    { $r = Util.MakeRule($str.text, $rule.action, $rule.context, $rule.excluded, $paramList.list); }
    ;

rule returns [List<IRuleSegment> action, RuleContext context, RuleContext excluded]:
    ruleAction { $action = $ruleAction.value; }
    (
        SLASH ( ruleContext { $context = $ruleContext.value; } SLASH? )?
        ( SLASH excludedContext { $excluded = $excludedContext.value; } )?
    )?
    ;

ruleAction returns [List<IRuleSegment> value]
    @init { 
        var left = new List<IMatrixMatcher>(); 
        var right = new List<IMatrixCombiner>(); 
    }: 
    (matchTerm { left.AddRange($matchTerm.value); })+
    ARROW 
    (actionTerm { right.AddRange($actionTerm.value); })+
    { $value = Util.MakeRuleAction(left, right); }
    ;

matchTerm returns [IEnumerable<IMatrixMatcher> value]:
        matchableMatrix { $value = new IMatrixMatcher[] { new MatrixMatcher($matchableMatrix.val) }; }
    |   symbolStr { $value = $symbolStr.slist.ConvertAll<IMatrixMatcher>(s => s); }
    |   NULL { $value = new IMatrixMatcher[] { null }; }
    ;

actionTerm returns [IEnumerable<IMatrixCombiner> value]: 
        combinableMatrix { $value = new IMatrixCombiner[] { new MatrixCombiner($combinableMatrix.val) }; }
    |   symbolStr { $value = $symbolStr.slist.ConvertAll<IMatrixCombiner>(s => s); }
    |   NULL { $value = new IMatrixCombiner[] { null }; }
    ;

ruleContext returns [RuleContext value]
    @init { $value = new RuleContext(); }:
    (lBound { $value.Left.Add($lBound.seg); })? 
    (leftContextTerm { $value.Left.AddRange($leftContextTerm.segs); })* 
    UNDERSCORE
    (rightContextTerm { $value.Right.AddRange($rightContextTerm.segs); })* 
    (rBound { $value.Right.Add($rBound.seg); })?
    ;

excludedContext returns [RuleContext value]:
    ruleContext
    { $value = $ruleContext.value; }
    ;

leftContextTerm returns [IEnumerable<IRuleSegment> segs]:
    contextTerm { $segs = $contextTerm.segs; }
    ;

rightContextTerm returns [IEnumerable<IRuleSegment> segs]:
    contextTerm { $segs = $contextTerm.segs; }
    ;

contextTerm returns [IEnumerable<IRuleSegment> segs]
        @init { $segs = new List<IRuleSegment>(); }:
        matchableMatrix 
        { $segs = new IRuleSegment[] { new FeatureMatrixSegment(new MatrixMatcher($matchableMatrix.val), MatrixCombiner.NullCombiner) }; }
    |   symbolStr
        { $segs = $symbolStr.slist.ConvertAll<IRuleSegment>(s => new FeatureMatrixSegment(s, MatrixCombiner.NullCombiner)); }
    ;

lBound returns [IRuleSegment seg]: 
    BOUNDARY { $seg = Word.LeftBoundary; } ;

rBound returns [IRuleSegment seg]: 
    BOUNDARY { $seg = Word.RightBoundary; } ;

/* Feature matrices */

matrix returns [FeatureMatrix fm] 
    @init { List<FeatureValue> fvList = new List<FeatureValue>(); }:
    LBRACE (featureVal { fvList.Add($featureVal.fv); })* RBRACE
    { $fm = new FeatureMatrix(fvList); }
    ;

matchableMatrix returns [IEnumerable<IMatchable> val]
    @init{ var fvList = new List<IMatchable>(); }:
    LBRACE 
    ( matchableVal { fvList.Add($matchableVal.fv); } )* 
    RBRACE
    { $val = fvList; }
    ;

combinableMatrix returns [IEnumerable<ICombinable> val]
    @init{ var fvList = new List<ICombinable>(); }:
    LBRACE 
    ( combinableVal { fvList.Add($combinableVal.fv); } )* 
    RBRACE
    { $val = fvList; }
    ;

/* Feature values */

featureVal returns [FeatureValue fv]: 
        scalarVal { $fv = $scalarVal.fv; }
    |   binaryVal { $fv = $binaryVal.fv; }
    |   unaryVal { $fv = $unaryVal.fv; }
    |   nullVal { $fv = $nullVal.fv; }
    ;

matchableVal returns [IMatchable fv]:
        scalarVal { $fv = $scalarVal.fv; }
    |   binaryVal { $fv = $binaryVal.fv; }
    |   bareVal { $fv = $bareVal.fv; }
    |   nullVal { $fv = $nullVal.fv; }
    |   variableVal { $fv = $variableVal.fv; }
    ;

combinableVal returns [ICombinable fv]:
        scalarVal { $fv = $scalarVal.fv; }
    |   binaryVal { $fv = $binaryVal.fv; }
    |   unaryVal { $fv = $unaryVal.fv; }
    |   nullVal { $fv = $nullVal.fv; }
    |   variableVal { $fv = $variableVal.fv; }
    ;

scalarVal returns [FeatureValue fv]: 
    scalarFeature EQ NUMBER { $fv = $scalarFeature.f.Value(Int32.Parse($NUMBER.text)); }
    ;

binaryVal returns [FeatureValue fv]: 
        (PLUS binaryFeature) { $fv = $binaryFeature.f.PlusValue; }
    |   (MINUS binaryFeature) { $fv = $binaryFeature.f.MinusValue; }
    ;

unaryVal returns [FeatureValue fv]: 
    unaryFeature { $fv = $unaryFeature.f.Value; };

nullVal returns [FeatureValue fv]:
    NULL feature { $fv = $feature.f.NullValue; };

variableVal returns [IMatchCombine fv]: 
    '$' feature { $fv = $feature.f.VariableValue; };

/* the bareVal can be a unary match or a node-exists match. it exists here to
 * help the parser handle the ambiguity that otherwise results. 
 */
bareVal returns [IMatchable fv]:
    feature 
    {
        if ($feature.f is NodeFeature) $fv = ($feature.f as NodeFeature).ExistsValue;
        else $fv = ($feature.f as UnaryFeature).Value;
    }
    ;

/* Parameters */

paramList returns [ParamList list]
    @init { $list = new ParamList(); }: 
    LPAREN 
    (param { $list.Add($param.str, $param.val); })* 
    RPAREN
    ;

param returns [string str, object val]: 
    label (EQ paramVal)?
    { $str = $label.text; $val = $paramVal.obj; }
    ;

paramVal returns [object obj]: 
        str { $obj = $str.text; }
    |   matrix  { $obj = $matrix.fm; }
    ;

# ToFLaC8
Восьмая лабораторная работа по теории формальных языков и компиляторов

Задание:  
Вариант 26 
Для грамматики G[<While>] разработать и реализовать алгоритм 
анализа на основе метода рекурсивного спуска. 
G[While]: 
1. While → do Stmt  while Cond ; 
2. Cond → LogExpr {or LogExpr} 
3. LogExpr → RelExpr {and RelExpr}  
4. RelExpr → Operand [rel Operand]
5. Operand → var | const 
6. Stmt → var as ArithExpr
7. ArithExpr → Operand {ao Operand}
Примечание: while, do, and, or – ключевые слова. В тип rel 
объединили операции сравнения <,<=, >=, >, != и ==, в тип ao 
арифметические операции + и -, в тип as оператор присваивания =, тип var – название переменной (только буквы), тип const – числа. Причина, по 
которой не объединены в один тип логические операции and и or 
заключается в том, что эти операции имеют различный приоритет.  Пример 
цепочки: while a < b and b <= c do b=b+c-20 ;

Язык L(G[While]) = {do var as (var | const)(ao(v|c))* while (v|c)(rel(v|c))* (and (v|c)(rel(v|c))*) *}, где var => перебор букв, const => числа, rel => операции сравнения, ao => +|-, as => =  
Классификация грамматики - Контекстно свободная (1, 2, 3, 4, 6 и 7 правила)  

Схема вызова функций:  
```
Parse()  
   |  
   +-- WhileStatement()  
        |  
        +-- Expect("do", "do")  
        |  
        +-- Stmt()  
        |    |  
        |    +-- IsVar()  
        |    |    |  
        |    |    +-- GetNextToken() (для peek)  
        |    |  
        |    +-- Consume() (если IsVar() true)  
        |    |  
        |    +-- Expect("as", "as", "statement")  
        |    |  
        |    +-- ArithExpr()  
        |         |  
        |         +-- Operand()  
        |         |    |  
        |         |    +-- IsVar()  
        |         |    |  
        |         |    +-- IsConst()  
        |         |    |  
        |         |    +-- GetNextToken() (для peek)  
        |         |    +-- Consume() (если IsVar() или IsConst() true)  
        |         |  
        |         +-- Match("ao")  (повторяется)  
        |  
        +-- Expect("while", "while")  
        |  
        +-- Cond()  
        |    |  
        |    +-- LogExpr()  
        |    |    |  
        |    |    +-- RelExpr()  
        |    |    |    |  
        |    |    |    +-- Operand() (как описано выше)  
        |    |    |    |  
        |    |    |    +-- Match("rel") (optional)  
        |    |    +-- Match("and") (повторяется)  
        |  
        +-- Expect(";", ";")    
        ```
![Снимок экрана (1958)](https://github.com/user-attachments/assets/8f2af8cd-623b-4aea-bb54-f306cc1a22f0)  
![Снимок экрана (1957)](https://github.com/user-attachments/assets/0df631b6-4d1e-4ef7-951f-587767ab762f)  
![Снимок экрана (1956)](https://github.com/user-attachments/assets/67b4ebb1-9d00-4787-b1a3-9a2138463a03)  
![Снимок экрана (1955)](https://github.com/user-attachments/assets/aef66a6b-8c10-46e8-b03b-e589aa6c9fc4)  






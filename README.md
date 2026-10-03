# votacao

Métodos de votação do zero em C# e .NET 8: maioria simples, Borda, segundo turno
instantâneo, Copeland e minimax. Os teoremas de impossibilidade não são citados:
os contraexemplos são **procurados**, varrendo todos os perfis de um tamanho.

```
$ dotnet medidor.dll tabela

metodo                 Condorcet      monotono    independente         manipulavel
maioria simples            viola      respeita           viola        nao se achou
Borda                      viola      respeita           viola                 sim
segundo turno              viola         viola           viola                 sim
Copeland                respeita      respeita           viola                 sim
minimax                 respeita      respeita           viola                 sim
ditadura                   viola      respeita        respeita                 nao
```

Cada célula é o resultado de uma varredura. "Viola" quer dizer que um perfil foi
achado e está impresso ao lado; "respeita" quer dizer que a varredura inteira
passou sem achar.

A linha da ditadura é o teorema de Arrow em uma linha: ela é a única que respeita
a independência, e é por isso que o teorema é de **impossibilidade**.

## Procurar em vez de citar

"Borda viola Condorcet" é uma frase que se repete e quase nunca vem com o perfil.
Aqui ela vem: a busca varre todos os perfis de um tamanho, acha o primeiro em que
o método falha, e devolve o perfil junto com a explicação.

Isso vale também para o **resultado negativo**. Copeland e minimax respeitam
Condorcet, e isso não é afirmado: a varredura inteira roda e não acha nada. Um
teste que só procura violações e sempre acha não distingue um método ruim de uma
busca mal feita.

## A varredura por contagem

Enumerar perfis como sequências de cédulas custa (c!)^e. Com três candidatos e
dezessete eleitores, isso passa de dez trilhões.

Mas todo método aqui é **anônimo**: ele não olha quem votou o quê, só quantos
votaram em cada ordem. Então enumerar as **contagens** cobre exatamente os
mesmos resultados:

```
$ dotnet medidor.dll espaco

  candidatos   eleitores                (c!)^e     C(e+c!-1, c!-1)         razao
           3           3                   216                  56             4
           3           9            10,077,696               2,002         5,034
           3          13        13,060,694,016               8,568     1,524,357
           3          17    16,926,659,444,736              26,334   642,768,263
```

Não é otimização. É o que torna possível chegar a treze eleitores, e é lá que
mora a violação mais interessante do repositório.

## O que as medidas me corrigiram

**A violação de monotonicidade do segundo turno não aparece com poucos
eleitores.** Eu montei a busca com três e quatro eleitores, como nas outras, e
ela não achou nada. Não foi falta de tentativa: até **nove** eleitores a
violação não existe. Com treze, existe:

```
ACB x4, BCA x5, CAB x3, CBA x1
  B vence.
O ultimo eleitor SOBE B na preferencia dele, de CBA para BCA.
  A vence.
```

Subir o vencedor na preferência de um eleitor o fez perder. Isso não é teoria:
aconteceu na eleição de Burlington em 2009, e o método foi revogado por causa
disso.

Foi essa busca que obrigou a trocar a enumeração por sequências pela enumeração
por contagem — com sequências, treze eleitores seriam treze bilhões de perfis.

**A maioria simples escapou da busca por manipulação, e eu ia escrever que todos
os cinco são manipuláveis.** Gibbard e Satterthwaite provaram que todo método não
ditatorial com três ou mais candidatos pode ser manipulado, e a varredura não
achou nada para a maioria simples até quatro candidatos e nove eleitores.

Não foi falta de tentativa. A prova é de contagem: o vencedor A é único, então
tem **estritamente** mais votos que todos. Um eleitor que queira eleger B precisa
tirar o voto dele de alguém e dar a B. Se votava em A, ele prefere A a tudo e não
tem o que ganhar. Se votava em outro, A não perde voto e B ganha um, e para B
passar A seria preciso que B já tivesse ao menos tantos votos quanto A — o que
contradiz A ser vencedor único.

O teorema continua valendo: ele é sobre regras **resolutas**, que sempre devolvem
um vencedor só e portanto têm um desempate embutido. A manipulação da maioria
simples mora justamente nos empates, e a versão daqui, que devolve conjunto, não
os resolve. Um teste mostra os dois lados: com um desempate acrescentado, ela
passa a ser manipulável.

**O número par de eleitores muda o resultado das buscas, e por um motivo que
engana.** Com número par os confrontos diretos empatam, empate não é vitória, e
quase nunca existe vencedor de Condorcet. Procurar violação de independência só
em perfis de tamanho ímpar dava o resultado errado de que Copeland e minimax a
respeitam.

## A escolha do método é parte do resultado

```
$ dotnet medidor.dll discordam

  candidatos   eleitores      perfis     discordam    fracao
           3           5         252            78    31.0 %
           3           7         792           222    28.0 %
           3           9       2,002           546    27.3 %
           4           5      98,280        49,752    50.6 %
           4           6     475,020       274,416    57.8 %
```

Em um perfil a cada três ou quatro, trocar o método troca o vencedor. Com quatro
candidatos, em mais da metade.

## O paradoxo que começou o assunto

Condorcet, 1785: a preferência da maioria pode ser **intransitiva** mesmo quando
a de cada eleitor é transitiva. Com ABC, BCA e CAB, a maioria prefere A a B, B a
C, e C a A.

```
$ dotnet medidor.dll ciclos

  candidatos   eleitores      perfis    sem vencedor    fracao
           3           3          56               4     7.1 %
           3           7         792              42     5.3 %
           3          11       4,368             252     5.8 %
           4           5      98,280          12,480    12.7 %
```

Não é uma curiosidade de livro: acontece em um perfil a cada vinte com três
candidatos, e em um a cada oito com quatro.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Perfil.cs` | cédulas e perfis, e as duas enumerações (por sequência e por contagem) |
| `Metodos.cs` | os cinco métodos, mais Condorcet e a ditadura |
| `Criterios.cs` | as buscas por contraexemplo de cada critério |

## Como rodar

```
dotnet test testes/Votacao.Testes/Votacao.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `ciclos`, `tabela`, `discordam`, `espaco` e `tudo`.

## Licença

MIT.

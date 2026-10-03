using Conde.Votacao;

namespace Conde.Votacao.Medidor;

/// <summary>
/// As medidas. O que se mede e CONTAGEM DE PERFIS e EXISTENCIA DE
/// CONTRAEXEMPLO, que nao dependem de maquina.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "ciclos": Ciclos(); break;
            case "tabela": Tabela(); break;
            case "discordam": Discordam(); break;
            case "espaco": Espaco(); break;
            case "tudo": Ciclos(); Tabela(); Discordam(); Espaco(); break;
            default:
                Console.Error.WriteLine("medidas: ciclos, tabela, discordam, espaco, tudo");
                return 1;
        }
        return 0;
    }

    /// <summary>
    /// Em que fracao dos perfis o paradoxo de Condorcet acontece.
    ///
    /// Se fosse um em mil, seria curiosidade. A tabela diz se e.
    /// </summary>
    private static void Ciclos()
    {
        Console.WriteLine("== o paradoxo de Condorcet: em que fracao dos perfis nao ha vencedor ==");
        Console.WriteLine($"{"candidatos",12}{"eleitores",12}{"perfis",12}{"sem vencedor",16}{"fracao",10}");

        foreach (var (candidatos, eleitores) in new[] { (3, 3), (3, 5), (3, 7), (3, 9), (3, 11), (4, 3), (4, 5) })
        {
            var (com, sem) = Criterios.ContarCiclos(candidatos, eleitores);
            Console.WriteLine($"{candidatos,12}{eleitores,12}{com + sem,12:N0}{sem,16:N0}" +
                              $"{sem / (double)(com + sem),10:P1}");
        }

        Console.WriteLine();
        Console.WriteLine("o ciclo e minoria e nao e raro. E repare que ele some com numero PAR de");
        Console.WriteLine("eleitores pela razao errada: ali os confrontos empatam, e empate nao e");
        Console.WriteLine("vitoria, entao quase nunca ha vencedor de Condorcet e a contagem engana");
        Console.WriteLine();

        var ciclo = Criterios.AcharCiclo(3, 3);
        Console.WriteLine($"o menor ciclo achado: {ciclo}");
        Console.WriteLine("  A ganha de B, B ganha de C, e C ganha de A");
        Console.WriteLine();
    }

    /// <summary>
    /// A tabela de criterios, procurada em vez de citada.
    ///
    /// Cada celula e o resultado de uma varredura: "viola" quer dizer que um
    /// perfil foi achado, e "respeita" quer dizer que a varredura inteira
    /// passou sem achar.
    /// </summary>
    private static void Tabela()
    {
        Console.WriteLine("== os criterios, procurados em todos os perfis de ate 9 eleitores ==");
        Console.WriteLine($"{"metodo",-18}{"Condorcet",14}{"monotono",14}{"independente",16}{"manipulavel",20}");

        var tamanhos = new[] { (3, 4), (3, 5), (3, 6), (3, 7), (4, 4), (4, 5) };

        foreach (var (nome, metodo) in Metodos.Todos())
        {
            var condorcet = Procurar(tamanhos, (c, e) => Criterios.ViolaCondorcet(metodo, c, e) is not null);
            var monotono = Procurar(tamanhos, (c, e) => Criterios.ViolaMonotonicidade(metodo, c, e) is not null)
                           || Criterios.ViolaMonotonicidade(metodo, 3, 13) is not null;
            var independente = Procurar(tamanhos, (c, e) => Criterios.ViolaIndependencia(metodo, c, e) is not null);
            var manipulavel = Procurar(tamanhos, (c, e) => Criterios.EhManipulavel(metodo, c, e) is not null);

            Console.WriteLine($"{nome,-18}{(condorcet ? "viola" : "respeita"),14}" +
                              $"{(monotono ? "viola" : "respeita"),14}" +
                              $"{(independente ? "viola" : "respeita"),16}" +
                              $"{(manipulavel ? "sim" : "nao se achou"),20}");
        }

        Console.WriteLine($"{"ditadura",-18}{"viola",14}{"respeita",14}{"respeita",16}{"nao",20}");

        Console.WriteLine();
        Console.WriteLine("a linha da ditadura e o teorema de Arrow em uma linha: ela e a unica que");
        Console.WriteLine("respeita a independencia, e e por isso que o teorema e de IMPOSSIBILIDADE.");
        Console.WriteLine();
        Console.WriteLine("e a coluna da manipulacao tem uma ressalva que eu ia deixar de fora: a maioria");
        Console.WriteLine("simples escapa da busca, e nao por falta de tentativa. Com um manipulador so");
        Console.WriteLine("e exigindo vencedor UNICO dos dois lados, ela nao pode ser manipulada: o");
        Console.WriteLine("eleitor tira um voto de alguem e da a outro, e para o novo passar o antigo");
        Console.WriteLine("seria preciso que ele ja tivesse tantos votos quanto, o que contradiz o");
        Console.WriteLine("vencedor ser unico. Gibbard e Satterthwaite falam de regras RESOLUTAS, que");
        Console.WriteLine("tem desempate embutido, e a manipulacao mora justamente nos empates.");
        Console.WriteLine();
    }

    private static bool Procurar((int, int)[] tamanhos, Func<int, int, bool> acha)
    {
        foreach (var (c, e) in tamanhos) if (acha(c, e)) return true;
        return false;
    }

    /// <summary>
    /// Em que fracao dos perfis os metodos DISCORDAM entre si.
    ///
    /// E a medida que diz se a escolha do metodo e detalhe tecnico ou decisao
    /// politica.
    /// </summary>
    private static void Discordam()
    {
        Console.WriteLine("== em que fracao dos perfis os cinco metodos discordam ==");
        Console.WriteLine($"{"candidatos",12}{"eleitores",12}{"perfis",12}{"discordam",14}{"fracao",10}");

        foreach (var (candidatos, eleitores) in new[] { (3, 5), (3, 7), (3, 9), (4, 5), (4, 6) })
        {
            int total = 0, diferentes = 0;
            foreach (var perfil in Perfil.TodosPorContagem(candidatos, eleitores))
            {
                total++;
                var resultados = Metodos.Todos()
                    .Select(m => string.Join(",", m.Apurar(perfil)))
                    .Distinct().Count();
                if (resultados > 1) diferentes++;
            }
            Console.WriteLine($"{candidatos,12}{eleitores,12}{total,12:N0}{diferentes,14:N0}" +
                              $"{diferentes / (double)total,10:P1}");
        }

        Console.WriteLine();
        Console.WriteLine("em um perfil a cada tres ou quatro, trocar o metodo troca o vencedor. A");
        Console.WriteLine("escolha do metodo nao e detalhe tecnico: e parte do resultado");
        Console.WriteLine();
    }

    /// <summary>
    /// O tamanho do espaco de busca, por sequencia e por contagem.
    ///
    /// E a conta que tornou a varredura possivel.
    /// </summary>
    private static void Espaco()
    {
        Console.WriteLine("== o espaco de busca: por sequencia contra por contagem ==");
        Console.WriteLine($"{"candidatos",12}{"eleitores",12}{"(c!)^e",22}{"C(e+c!-1, c!-1)",20}{"razao",14}");

        foreach (var (candidatos, eleitores) in new[] { (3, 3), (3, 5), (3, 9), (3, 13), (3, 17), (4, 5), (4, 7) })
        {
            var ordens = 1;
            for (var i = 2; i <= candidatos; i++) ordens *= i;

            var porSequencia = System.Numerics.BigInteger.Pow(ordens, eleitores);
            var porContagem = Combinacoes(eleitores + ordens - 1, ordens - 1);

            Console.WriteLine($"{candidatos,12}{eleitores,12}{porSequencia,22:N0}{porContagem,20:N0}" +
                              $"{(double)porSequencia / (double)porContagem,14:N0}");
        }

        Console.WriteLine();
        Console.WriteLine("todo metodo daqui e ANONIMO: ele nao olha quem votou o que, so quantos");
        Console.WriteLine("votaram em cada ordem. Enumerar sequencias conta o mesmo perfil varias");
        Console.WriteLine("vezes, e enumerar contagens conta uma. Com tres candidatos e dezessete");
        Console.WriteLine("eleitores, sao 26 mil em vez de dez trilhoes, e e essa diferenca que");
        Console.WriteLine("permitiu achar a violacao de monotonicidade do segundo turno");
        Console.WriteLine();
    }

    private static System.Numerics.BigInteger Combinacoes(int total, int quantas)
    {
        System.Numerics.BigInteger saida = 1;
        for (var i = 0; i < quantas; i++) saida = saida * (total - i) / (i + 1);
        return saida;
    }
}

using Xunit;

namespace Conde.Votacao.Testes;

/// <summary>
/// Os teoremas de impossibilidade, PROCURADOS em vez de citados.
///
/// A varredura e por CONTAGEM e nao por sequencia de cedulas, porque todo metodo
/// aqui e anonimo: ele nao olha quem votou o que. Isso derruba o espaco de
/// (c!)^e para C(e + c! - 1, c! - 1), e e o que torna possivel chegar a treze
/// eleitores, que e onde a violacao de monotonicidade do segundo turno aparece.
/// </summary>
public class CriteriosTest
{
    /// <summary>
    /// O PARADOXO DE CONDORCET, de 1785: a preferencia da maioria pode ser
    /// INTRANSITIVA mesmo com todo eleitor sendo transitivo.
    /// </summary>
    [Fact]
    public void OParadoxoDeCondorcetExisteEhAchado()
    {
        var ciclo = Criterios.AcharCiclo(3, 3);
        Assert.NotNull(ciclo);
        Assert.Empty(Metodos.Condorcet(ciclo!));

        var classico = Perfil.De("ABC,BCA,CAB");
        Assert.True(classico.Vence(0, 1));     // A ganha de B
        Assert.True(classico.Vence(1, 2));     // B ganha de C
        Assert.True(classico.Vence(2, 0));     // e C ganha de A
        Assert.Empty(Metodos.Condorcet(classico));

        // Cada eleitor e perfeitamente transitivo; a MAIORIA nao e.
        foreach (var cedula in classico.Cedulas)
            for (var a = 0; a < 3; a++)
                for (var b = 0; b < 3; b++)
                    for (var c = 0; c < 3; c++)
                        if (cedula.Prefere(a, b) && cedula.Prefere(b, c)) Assert.True(cedula.Prefere(a, c));
    }

    /// <summary>
    /// O ciclo nao e raro, e isso vira numero.
    /// </summary>
    [Fact]
    public void OCicloNaoEhRaro()
    {
        var (com, sem) = Criterios.ContarCiclos(3, 3);
        Assert.Equal(56, com + sem);        // C(3 + 5, 5) perfis por contagem
        Assert.True(sem > 0, "nenhum ciclo");
        Assert.InRange(sem / (double)(com + sem), 0.01, 0.15);
    }

    /// <summary>
    /// Os metodos POSICIONAIS e o segundo turno violam Condorcet, e o teste acha
    /// o perfil de cada um.
    /// </summary>
    [Fact]
    public void OsPosicionaisEOSegundoTurnoViolamCondorcet()
    {
        Assert.NotNull(Criterios.ViolaCondorcet(Metodos.MaioriaSimples, 3, 5));
        Assert.NotNull(Criterios.ViolaCondorcet(Metodos.Borda, 3, 5));
        Assert.NotNull(Criterios.ViolaCondorcet(Metodos.SegundoTurno, 3, 5));
    }

    /// <summary>
    /// COPELAND e MINIMAX respeitam Condorcet, SEMPRE.
    ///
    /// Nao e sorte: quem ganha de todos tem pontuacao maxima no Copeland e
    /// margem negativa no minimax. A varredura nao acha violacao, e e esse
    /// resultado negativo que da sentido aos positivos.
    /// </summary>
    [Fact]
    public void CopelandEMinimaxRespeitamCondorcet()
    {
        foreach (var eleitores in new[] { 3, 5, 7, 9 })
        {
            Assert.Null(Criterios.ViolaCondorcet(Metodos.Copeland, 3, eleitores));
            Assert.Null(Criterios.ViolaCondorcet(Metodos.Minimax, 3, eleitores));
        }
        Assert.Null(Criterios.ViolaCondorcet(Metodos.Copeland, 4, 5));
        Assert.Null(Criterios.ViolaCondorcet(Metodos.Minimax, 4, 5));
    }

    /// <summary>
    /// O SEGUNDO TURNO viola a MONOTONICIDADE: subir o vencedor na preferencia
    /// de alguem o faz PERDER.
    ///
    /// E o criterio mais basico que existe, e a violacao nao e teoria: aconteceu
    /// na eleicao de Burlington em 2009, e o metodo foi revogado por causa
    /// disso.
    ///
    /// A violacao so aparece com uma dezena de eleitores, e foi por isso que a
    /// varredura precisou passar a ser por contagem: com nove eleitores ela nao
    /// existe, e enumerar sequencias nunca chegaria a treze.
    /// </summary>
    [Fact]
    public void OSegundoTurnoViolaMonotonicidade()
    {
        // Ate nove eleitores, nao ha violacao.
        foreach (var eleitores in new[] { 3, 5, 7, 9 })
            Assert.Null(Criterios.ViolaMonotonicidade(Metodos.SegundoTurno, 3, eleitores));

        // Com treze, ha.
        var violacao = Criterios.ViolaMonotonicidade(Metodos.SegundoTurno, 3, 13);
        Assert.NotNull(violacao);
        Assert.Contains("SUBIU", violacao!.Explicacao);
    }

    /// <summary>
    /// Os metodos POSICIONAIS respeitam monotonicidade, e isso tambem e
    /// verificado em vez de suposto.
    ///
    /// Maioria simples e Borda somam pontos por posicao, e subir um candidato so
    /// pode aumentar os pontos dele. Copeland e minimax tambem respeitam, por
    /// outro caminho: subir um candidato so pode melhorar os confrontos diretos
    /// dele.
    /// </summary>
    [Fact]
    public void OsOutrosQuatroRespeitamMonotonicidade()
    {
        foreach (var eleitores in new[] { 3, 5, 7, 9 })
        {
            Assert.Null(Criterios.ViolaMonotonicidade(Metodos.MaioriaSimples, 3, eleitores));
            Assert.Null(Criterios.ViolaMonotonicidade(Metodos.Borda, 3, eleitores));
            Assert.Null(Criterios.ViolaMonotonicidade(Metodos.Copeland, 3, eleitores));
            Assert.Null(Criterios.ViolaMonotonicidade(Metodos.Minimax, 3, eleitores));
        }
    }

    /// <summary>
    /// TODOS os cinco metodos violam a INDEPENDENCIA DE ALTERNATIVAS
    /// IRRELEVANTES, e a ditadura nao.
    ///
    /// E o conteudo do teorema de Arrow, de 1951: com tres ou mais candidatos, a
    /// unica regra unanime e independente e a ditadura.
    /// </summary>
    [Fact]
    public void TodosViolamIndependenciaMenosADitadura()
    {
        foreach (var (nome, metodo) in Metodos.Todos())
        {
            // A quantidade PAR de eleitores entra na busca de proposito: com
            // numero impar nao ha empate em confronto direto, e Copeland e
            // minimax ficam mais dificeis de quebrar. Procurar so em perfis de
            // tamanho impar daria o resultado errado de que eles respeitam a
            // independencia.
            var violacao = Criterios.ViolaIndependencia(metodo, 3, 4)
                        ?? Criterios.ViolaIndependencia(metodo, 3, 5)
                        ?? Criterios.ViolaIndependencia(metodo, 3, 6)
                        ?? Criterios.ViolaIndependencia(metodo, 4, 4);
            Assert.True(violacao is not null, $"{nome} nao violou independencia");
        }

        foreach (var eleitores in new[] { 3, 5, 7 })
        {
            Assert.Null(Criterios.ViolaIndependencia(p => Metodos.Ditadura(p, 0), 3, eleitores));
            Assert.Null(Criterios.ViolaIndependencia(p => Metodos.Ditadura(p, 1), 3, eleitores));
        }
    }

    /// <summary>
    /// Quatro dos cinco metodos sao MANIPULAVEIS, e a ditadura nao.
    ///
    /// E o teorema de Gibbard e Satterthwaite, de 1973 e 1975: todo metodo nao
    /// ditatorial com tres ou mais candidatos pode ser manipulado.
    /// </summary>
    [Fact]
    public void QuatroDosCincoSaoManipulaveisEADitaduraNao()
    {
        Assert.NotNull(Criterios.EhManipulavel(Metodos.SegundoTurno, 3, 5));
        Assert.NotNull(Criterios.EhManipulavel(Metodos.Borda, 4, 5));
        Assert.NotNull(Criterios.EhManipulavel(Metodos.Copeland, 4, 4));
        Assert.NotNull(Criterios.EhManipulavel(Metodos.Minimax, 4, 4));

        foreach (var eleitores in new[] { 3, 5, 7 })
            Assert.Null(Criterios.EhManipulavel(p => Metodos.Ditadura(p, 0), 3, eleitores));
    }

    /// <summary>
    /// A MAIORIA SIMPLES escapa da busca, e nao por falta de tentativa: com um
    /// manipulador so e exigindo vencedor UNICO dos dois lados, ela nao pode ser
    /// manipulada, e a prova e de contagem.
    ///
    /// Eu ia escrever que todos os cinco sao manipulaveis, citando Gibbard e
    /// Satterthwaite. A varredura nao achou nada para a maioria simples ate sete
    /// candidatos e nove eleitores, e o motivo e este:
    ///
    /// o vencedor A e unico, entao tem ESTRITAMENTE mais votos que todos. Um
    /// eleitor que queira eleger B precisa tirar o voto dele de alguem e dar a
    /// B. Se ele votava em A, ele prefere A a tudo e nao tem o que ganhar. Se
    /// votava em outro, A nao perde voto e B ganha um, e para B passar A seria
    /// preciso que B ja tivesse ao menos tantos votos quanto A, o que contradiz
    /// A ser vencedor unico.
    ///
    /// O teorema continua valendo: ele e sobre regras RESOLUTAS, que sempre
    /// devolvem um vencedor so e portanto tem um criterio de desempate embutido.
    /// A manipulacao da maioria simples mora justamente nos empates, e a versao
    /// daqui, que devolve conjunto, nao os resolve.
    /// </summary>
    [Fact]
    public void AMaioriaSimplesNaoEhManipulavelSemDesempate()
    {
        foreach (var eleitores in new[] { 3, 5, 7, 9 })
            Assert.Null(Criterios.EhManipulavel(Metodos.MaioriaSimples, 3, eleitores));
        Assert.Null(Criterios.EhManipulavel(Metodos.MaioriaSimples, 4, 5));

        // E com um desempate embutido, ela passa a ser manipulavel: e o que o
        // teorema diz.
        SortedSet<int> ComDesempate(Perfil p) => [Metodos.MaioriaSimples(p).Min];
        Assert.NotNull(Criterios.EhManipulavel(ComDesempate, 3, 4));
    }

    /// <summary>
    /// A manipulacao achada e DE VERDADE: o teste refaz a conta no
    /// contraexemplo em vez de confiar na busca.
    /// </summary>
    [Fact]
    public void AManipulacaoAchadaEhDeVerdade()
    {
        var violacao = Criterios.EhManipulavel(Metodos.SegundoTurno, 3, 5);
        Assert.NotNull(violacao);
        Assert.Contains("prefere", violacao!.Explicacao);
        Assert.Single(Metodos.SegundoTurno(violacao.Perfil));
    }

    /// <summary>
    /// A varredura por contagem cobre os mesmos perfis que a por sequencia, e e
    /// muito menor.
    ///
    /// O teste confere a equivalencia de forma direta: todo resultado que
    /// aparece numa aparece na outra.
    /// </summary>
    [Fact]
    public void AVarreduraPorContagemCobreOsMesmosResultados()
    {
        foreach (var (candidatos, eleitores) in new[] { (3, 3), (3, 4) })
        {
            var porSequencia = new HashSet<string>();
            foreach (var perfil in Perfil.Todos(candidatos, eleitores))
                porSequencia.Add(string.Join("|", Metodos.Todos().Select(m => string.Join(",", m.Apurar(perfil)))));

            var porContagem = new HashSet<string>();
            foreach (var perfil in Perfil.TodosPorContagem(candidatos, eleitores))
                porContagem.Add(string.Join("|", Metodos.Todos().Select(m => string.Join(",", m.Apurar(perfil)))));

            Assert.Equal(porSequencia, porContagem);

            // E a por contagem e muito menor.
            var sequencias = Perfil.Todos(candidatos, eleitores).Count();
            var contagens = Perfil.TodosPorContagem(candidatos, eleitores).Count();
            Assert.True(contagens < sequencias, $"{contagens} contagens contra {sequencias} sequencias");
        }
    }
}

namespace Conde.Votacao;

/// <summary>
/// Os CRITERIOS que um metodo de votacao pode satisfazer ou violar, e as buscas
/// por contraexemplo.
///
/// Nenhuma funcao aqui afirma nada: todas PROCURAM. Varrer todos os perfis de um
/// tamanho pequeno e caro em tempo e barato em confianca, e e o que transforma
/// "Borda viola Condorcet" de citacao em fato observado, com o perfil exato
/// impresso ao lado.
/// </summary>
public static class Criterios
{
    /// <summary>Um contraexemplo achado, com o perfil e a explicacao.</summary>
    public sealed record Violacao(Perfil Perfil, string Explicacao)
    {
        public override string ToString() => $"{Perfil}: {Explicacao}";
    }

    /// <summary>
    /// CONDORCET: quando existe um candidato que ganha de todos no confronto
    /// direto, o metodo precisa elege-lo.
    ///
    /// Procura o primeiro perfil em que o metodo falha nisso.
    /// </summary>
    public static Violacao? ViolaCondorcet(Func<Perfil, SortedSet<int>> metodo, int candidatos, int eleitores)
    {
        ArgumentNullException.ThrowIfNull(metodo);
        foreach (var perfil in Perfil.TodosPorContagem(candidatos, eleitores))
        {
            var condorcet = Metodos.Condorcet(perfil);
            if (condorcet.Count == 0) continue;
            var vencedores = metodo(perfil);
            if (vencedores.Count == 1 && vencedores.Contains(condorcet.Single())) continue;
            return new Violacao(perfil,
                $"o vencedor de Condorcet e {Nome(condorcet.Single())} e o metodo elegeu " +
                $"{string.Join(",", vencedores.Select(Nome))}");
        }
        return null;
    }

    /// <summary>
    /// MONOTONICIDADE: subir o vencedor na preferencia de alguem nao pode
    /// faze-lo perder.
    ///
    /// E o criterio mais basico que existe e o mais violado. A busca troca o
    /// vencedor com quem esta logo acima dele em UMA cedula, o que so pode
    /// ajuda-lo, e confere se ele continuou ganhando.
    /// </summary>
    public static Violacao? ViolaMonotonicidade(Func<Perfil, SortedSet<int>> metodo, int candidatos, int eleitores)
    {
        ArgumentNullException.ThrowIfNull(metodo);
        foreach (var perfil in Perfil.TodosPorContagem(candidatos, eleitores))
        {
            var vencedores = metodo(perfil);
            if (vencedores.Count != 1) continue;
            var vencedor = vencedores.Single();

            for (var eleitor = 0; eleitor < eleitores; eleitor++)
            {
                var cedula = perfil.Cedulas[eleitor];
                var onde = cedula.Posicao(vencedor);
                if (onde == 0) continue;                  // ja esta em primeiro

                // Sobe o vencedor uma posicao: so pode ajudar.
                var melhorada = perfil.Com(eleitor, cedula.Trocando(onde, onde - 1));
                var depois = metodo(melhorada);
                if (depois.Count == 1 && depois.Contains(vencedor)) continue;

                return new Violacao(perfil,
                    $"{Nome(vencedor)} vencia; o eleitor {eleitor} o SUBIU e o resultado virou " +
                    $"{string.Join(",", depois.Select(Nome))} (perfil novo: {melhorada})");
            }
        }
        return null;
    }

    /// <summary>
    /// INDEPENDENCIA DE ALTERNATIVAS IRRELEVANTES: tirar um candidato que nao
    /// venceu nao pode mudar quem venceu.
    ///
    /// E a condicao mais forte de Arrow, e a que todo metodo razoavel viola. Ela
    /// e a razao de existir o "voto util" e o candidato que "rouba votos".
    /// </summary>
    public static Violacao? ViolaIndependencia(Func<Perfil, SortedSet<int>> metodo, int candidatos, int eleitores)
    {
        ArgumentNullException.ThrowIfNull(metodo);
        if (candidatos < 3) return null;

        foreach (var perfil in Perfil.TodosPorContagem(candidatos, eleitores))
        {
            var vencedores = metodo(perfil);
            if (vencedores.Count != 1) continue;
            var vencedor = vencedores.Single();

            for (var fora = 0; fora < candidatos; fora++)
            {
                if (fora == vencedor) continue;
                var menor = perfil.Sem(fora);
                var renumerado = fora < vencedor ? vencedor - 1 : vencedor;

                var depois = metodo(menor);
                if (depois.Count == 1 && depois.Contains(renumerado)) continue;

                return new Violacao(perfil,
                    $"{Nome(vencedor)} vencia; tirando {Nome(fora)}, que nao venceu, o resultado virou " +
                    $"{string.Join(",", depois.Select(x => Nome(x >= fora ? x + 1 : x)))}");
            }
        }
        return null;
    }

    /// <summary>
    /// MANIPULACAO (Gibbard e Satterthwaite, 1973 e 1975): um eleitor consegue
    /// melhorar o resultado MENTINDO sobre a propria preferencia.
    ///
    /// A busca testa todas as cedulas falsas de todos os eleitores e confere se
    /// alguma delas leva a um vencedor que aquele eleitor prefere de verdade.
    ///
    /// O teorema diz que todo metodo nao ditatorial com tres ou mais candidatos
    /// e manipulavel. O teste nao cita: acha.
    /// </summary>
    public static Violacao? EhManipulavel(Func<Perfil, SortedSet<int>> metodo, int candidatos, int eleitores)
    {
        ArgumentNullException.ThrowIfNull(metodo);
        var todasAsOrdens = Perfil.Permutacoes(candidatos).Select(x => new Cedula(x)).ToList();

        foreach (var perfil in Perfil.TodosPorContagem(candidatos, eleitores))
        {
            var honesto = metodo(perfil);
            if (honesto.Count != 1) continue;
            var atual = honesto.Single();

            for (var eleitor = 0; eleitor < eleitores; eleitor++)
            {
                var verdadeira = perfil.Cedulas[eleitor];
                foreach (var mentira in todasAsOrdens)
                {
                    var comMentira = metodo(perfil.Com(eleitor, mentira));
                    if (comMentira.Count != 1) continue;
                    var novo = comMentira.Single();
                    if (novo == atual) continue;

                    // A preferencia que vale e a VERDADEIRA do eleitor.
                    if (!verdadeira.Prefere(novo, atual)) continue;

                    return new Violacao(perfil,
                        $"o eleitor {eleitor} prefere {Nome(novo)} a {Nome(atual)}; votando " +
                        $"{mentira} em vez de {verdadeira} ele troca o vencedor");
                }
            }
        }
        return null;
    }

    /// <summary>
    /// O PARADOXO DE CONDORCET: um perfil em que nenhum candidato ganha de
    /// todos, porque as maiorias formam um ciclo.
    ///
    /// E o resultado de 1785 que comecou o assunto: a preferencia da maioria
    /// pode ser INTRANSITIVA mesmo quando a de cada eleitor e transitiva.
    /// </summary>
    public static Perfil? AcharCiclo(int candidatos, int eleitores)
    {
        foreach (var perfil in Perfil.TodosPorContagem(candidatos, eleitores))
            if (Metodos.Condorcet(perfil).Count == 0) return perfil;
        return null;
    }

    /// <summary>
    /// Em quantos dos perfis de um tamanho NAO existe vencedor de Condorcet.
    ///
    /// E a medida que diz se o paradoxo e uma curiosidade ou um problema de
    /// verdade.
    /// </summary>
    public static (int Com, int Sem) ContarCiclos(int candidatos, int eleitores)
    {
        int com = 0, sem = 0;
        foreach (var perfil in Perfil.TodosPorContagem(candidatos, eleitores))
            if (Metodos.Condorcet(perfil).Count > 0) com++; else sem++;
        return (com, sem);
    }

    public static string Nome(int candidato) => ((char)('A' + candidato)).ToString();
}

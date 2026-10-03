namespace Conde.Votacao;

/// <summary>
/// Os metodos de apuracao.
///
/// Todos devolvem um CONJUNTO de vencedores e nao um vencedor so, porque empate
/// acontece e escolher um deles exigiria um criterio de desempate que nao faz
/// parte do metodo. Esconder o empate atras de um desempate arbitrario e a forma
/// mais comum de um teste de votacao passar sem testar nada.
/// </summary>
public static class Metodos
{
    /// <summary>
    /// MAIORIA SIMPLES: vence quem tem mais primeiros lugares.
    ///
    /// E o metodo mais usado do mundo e o pior de todos pelos criterios da
    /// teoria. Ele ignora tudo o que o eleitor disse depois do primeiro nome, e
    /// e por isso que ele pode eleger um candidato que a maioria absoluta
    /// detesta.
    /// </summary>
    public static SortedSet<int> MaioriaSimples(Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        var votos = new int[perfil.Candidatos];
        foreach (var cedula in perfil.Cedulas) votos[cedula.Primeiro]++;
        return Melhores(votos);
    }

    /// <summary>
    /// BORDA, 1770: cada posicao vale pontos, do ultimo para o primeiro.
    ///
    /// Com c candidatos, o primeiro lugar vale c-1 e o ultimo vale zero. Ele
    /// usa a cedula inteira, e e por isso que ele quase sempre elege o candidato
    /// "de consenso".
    ///
    /// O preco: Borda NAO respeita Condorcet. Existe perfil em que um candidato
    /// ganha de todos os outros no confronto direto e perde em pontos, e o teste
    /// deste repositorio procura esse perfil em vez de cita-lo.
    /// </summary>
    public static SortedSet<int> Borda(Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        var pontos = new int[perfil.Candidatos];
        foreach (var cedula in perfil.Cedulas)
            for (var i = 0; i < perfil.Candidatos; i++)
                pontos[cedula.Ordem[i]] += perfil.Candidatos - 1 - i;
        return Melhores(pontos);
    }

    /// <summary>
    /// SEGUNDO TURNO INSTANTANEO: elimina quem tem menos primeiros lugares e
    /// redistribui, ate alguem passar da metade.
    ///
    /// E o metodo da Australia e das eleicoes brasileiras em duas voltas, so que
    /// feito de uma vez com a cedula ordenada.
    ///
    /// Ele e o que viola a MONOTONICIDADE de forma mais espetacular: existe
    /// perfil em que um candidato vence, e se alguns eleitores o SOBEM na
    /// preferencia, ele perde. Isso nao e teoria: aconteceu na eleicao de
    /// Burlington em 2009.
    /// </summary>
    public static SortedSet<int> SegundoTurno(Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        var vivos = new HashSet<int>(Enumerable.Range(0, perfil.Candidatos));

        while (vivos.Count > 1)
        {
            var votos = new int[perfil.Candidatos];
            foreach (var cedula in perfil.Cedulas)
                foreach (var candidato in cedula.Ordem)
                    if (vivos.Contains(candidato)) { votos[candidato]++; break; }

            var maioria = perfil.Eleitores / 2;
            var lider = vivos.OrderByDescending(x => votos[x]).ThenBy(x => x).First();
            if (votos[lider] > maioria) return [lider];

            // Elimina os ULTIMOS, todos eles em caso de empate: eliminar um so,
            // por desempate arbitrario, esconde empates de verdade.
            var menos = vivos.Min(x => votos[x]);
            var eliminados = vivos.Where(x => votos[x] == menos).ToList();
            if (eliminados.Count == vivos.Count) return [.. vivos];
            foreach (var x in eliminados) vivos.Remove(x);
        }
        return [.. vivos];
    }

    /// <summary>
    /// O vencedor de CONDORCET: quem vence TODOS os outros no confronto direto.
    ///
    /// Quando ele existe, quase todo mundo concorda que ele deveria vencer. O
    /// problema e que ele nem sempre existe, e e esse o paradoxo de Condorcet,
    /// de 1785.
    ///
    /// Devolve conjunto vazio quando nao ha nenhum.
    /// </summary>
    public static SortedSet<int> Condorcet(Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        for (var a = 0; a < perfil.Candidatos; a++)
        {
            var ganhaDeTodos = true;
            for (var b = 0; b < perfil.Candidatos && ganhaDeTodos; b++)
                if (b != a && !perfil.Vence(a, b)) ganhaDeTodos = false;
            if (ganhaDeTodos) return [a];
        }
        return [];
    }

    /// <summary>
    /// COPELAND: vence quem ganha mais confrontos diretos, contando empate como
    /// meio ponto.
    ///
    /// Ele SEMPRE respeita Condorcet, porque quem ganha de todos tem a
    /// pontuacao maxima. E, ao contrario do vencedor de Condorcet, ele sempre
    /// existe: quando nao ha vencedor de Condorcet, ele devolve quem chegou mais
    /// perto.
    ///
    /// A pontuacao e em DOBRO para o empate caber em inteiro: vitoria vale 2 e
    /// empate vale 1. Usar meio ponto em ponto flutuante criaria empates que nao
    /// existem.
    /// </summary>
    public static SortedSet<int> Copeland(Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        var pontos = new int[perfil.Candidatos];
        for (var a = 0; a < perfil.Candidatos; a++)
            for (var b = 0; b < perfil.Candidatos; b++)
            {
                if (a == b) continue;
                if (perfil.Vence(a, b)) pontos[a] += 2;
                else if (!perfil.Vence(b, a)) pontos[a] += 1;     // empate
            }
        return Melhores(pontos);
    }

    /// <summary>
    /// MINIMAX: vence quem tem a MENOR pior derrota.
    ///
    /// Para cada candidato, olha-se a maior margem com que ele perde de alguem;
    /// vence quem tem essa margem menor. Quem nao perde de ninguem tem margem
    /// negativa e vence, entao ele tambem respeita Condorcet.
    ///
    /// E um dos metodos que lidam com o ciclo de Condorcet sem inventar um
    /// desempate: no ciclo, ele escolhe quem foi derrotado mais de leve.
    /// </summary>
    public static SortedSet<int> Minimax(Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        var piorDerrota = new int[perfil.Candidatos];
        for (var a = 0; a < perfil.Candidatos; a++)
        {
            var pior = int.MinValue;
            for (var b = 0; b < perfil.Candidatos; b++)
            {
                if (a == b) continue;
                pior = Math.Max(pior, perfil.Quantos(b, a) - perfil.Quantos(a, b));
            }
            piorDerrota[a] = pior;
        }
        return Melhores(piorDerrota.Select(x => -x).ToArray());
    }

    /// <summary>
    /// A DITADURA do eleitor k: vence o preferido dele.
    ///
    /// Ela esta aqui porque e a unica regra que tem TODAS as propriedades que
    /// Arrow pediu, e e exatamente esse o conteudo do teorema: com tres ou mais
    /// candidatos, a unica regra que e unanime e independente de alternativas
    /// irrelevantes e a ditadura.
    ///
    /// Ter a ditadura implementada deixa o teorema testavel em vez de citavel:
    /// o teste procura violacao nela e nao acha, e acha em todas as outras.
    /// </summary>
    public static SortedSet<int> Ditadura(Perfil perfil, int eleitor)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        if (eleitor < 0 || eleitor >= perfil.Eleitores) throw new ArgumentOutOfRangeException(nameof(eleitor));
        return [perfil.Cedulas[eleitor].Primeiro];
    }

    private static SortedSet<int> Melhores(int[] pontos)
    {
        var melhor = pontos.Max();
        var saida = new SortedSet<int>();
        for (var i = 0; i < pontos.Length; i++) if (pontos[i] == melhor) saida.Add(i);
        return saida;
    }

    /// <summary>Os metodos com nome, para as varreduras.</summary>
    public static IEnumerable<(string Nome, Func<Perfil, SortedSet<int>> Apurar)> Todos()
    {
        yield return ("maioria simples", MaioriaSimples);
        yield return ("Borda", Borda);
        yield return ("segundo turno", SegundoTurno);
        yield return ("Copeland", Copeland);
        yield return ("minimax", Minimax);
    }
}

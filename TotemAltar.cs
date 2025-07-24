// TotemAltar.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TotemAltar : MonoBehaviour
{
    [Header("Configurações do Altar")]
    [Tooltip("ID único deste altar (deve corresponder ao ID do totem)")]
    public string altarID = "totem_01";
    
    [Tooltip("Nome do altar para exibição")]
    public string altarName = "Altar Antigo";
    
    [Header("Textos de Interação")]
    public string placeTotemText = "Colocar Totem";
    public string pourOilText = "Derramar Óleo";
    public string burnTotemText = "Queimar Totem";
    public string needOilText = "Precisa de um jarro de óleo";
    public string wrongTotemText = "Totem incorreto para este altar";
    
    [Header("Configurações de Óleo")]
    [Tooltip("Tempo para derramar o óleo")]
    public float pourOilTime = 3f;
    
    [Tooltip("Tempo para queimar o totem")]
    public float burnTime = 2f;
    
    [Tooltip("Tempo que o fogo fica aceso antes de apagar")]
    public float fireDuration = 2f;
    
    [Tooltip("O óleo desce durante a queima")]
    public bool oilDescendsWhileBurning = true;
    
    [Header("Configurações de Totem")]
    [Tooltip("Arraste o GameObject do olho esquerdo aqui")]
    public GameObject leftEyeObject;
    
    [Tooltip("Arraste o GameObject do olho direito aqui")]
    public GameObject rightEyeObject;
    
    [Tooltip("Portão que será destruído após queimar o totem")]
    public GameObject gateToDestroy;
    
    [Header("Referências do Altar")]
    [Tooltip("Transform onde o totem será colocado")]
    public Transform totemSlot;
    
    [Tooltip("Collider principal para interação (deixe vazio para usar o do próprio objeto)")]
    public Collider mainInteractionCollider;
    
    [Tooltip("Plane que simula o óleo")]
    public Transform oilPlane;
    
    [Tooltip("Altura inicial do plane de óleo")]
    public float oilPlaneStartY = -0.5f;
    
    [Tooltip("Altura final do plane de óleo")]
    public float oilPlaneEndY = 0f;
    
    [Tooltip("Partículas de fogo")]
    public ParticleSystem fireParticles;
    
    [Tooltip("Partículas de fumaça (ativada após o fogo)")]
    public ParticleSystem smokeParticles;
    
    [Tooltip("Tempo de fade do emission do fogo")]
    public float fireFadeDuration = 2f;
    
    [Tooltip("Distância que o fogo desce durante o fade (para simular diminuição)")]
    public float fireDescentDistance = 0.5f;
    
    [Tooltip("Tempo que a fumaça fica ativa antes de começar a fade")]
    public float smokeDuration = 3f;
    
    [Tooltip("Tempo de fade do emission da fumaça")]
    public float smokeFadeDuration = 2f;
    
    [Header("Estados do Altar")]
    [SerializeField] private bool hasTotem = false;
    [SerializeField] private bool hasOil = false;
    [SerializeField] private bool isBurned = false;
    [SerializeField] private bool isPouring = false;
    [SerializeField] private bool isBurning = false;
    
    private TotemPickup currentTotem;
    private float currentActionProgress = 0f;
    private ManualInteractionSystem manualSystem;
    private OilJar currentOilJar; // Referência para animar o jarro
    
    void Start()
    {
        manualSystem = GetComponent<ManualInteractionSystem>();
        
        // Configurar collider principal se não foi definido
        if (mainInteractionCollider == null)
        {
            mainInteractionCollider = GetComponent<Collider>();
            if (mainInteractionCollider == null)
            {
                Debug.LogWarning($"TotemAltar {altarName}: Nenhum collider encontrado! Adicione um Collider ou defina mainInteractionCollider.");
            }
        }
        
        // Garantir que totemSlot filhos não interferem na detecção
        if (totemSlot != null)
        {
            // Desabilitar colliders nos filhos do totemSlot para evitar interferência
            Collider[] childColliders = totemSlot.GetComponentsInChildren<Collider>();
            foreach (Collider childCol in childColliders)
            {
                if (childCol != mainInteractionCollider)
                {
                    childCol.enabled = false;
                    Debug.Log($"TotemAltar {altarName}: Desabilitando collider filho {childCol.name} para evitar interferência na detecção.");
                }
            }
        }
        
        // Configurar estado inicial
        if (oilPlane != null)
        {
            Vector3 pos = oilPlane.localPosition;
            pos.y = oilPlaneStartY;
            oilPlane.localPosition = pos;
            oilPlane.gameObject.SetActive(false);
        }
        
        if (fireParticles != null)
            fireParticles.Stop();
            
        if (smokeParticles != null)
            smokeParticles.Stop();
        
        UpdateInteractionText();
    }
    
    void Update()
    {
        // Atualizar texto de interação em tempo real
        UpdateInteractionText();
    }
    
    void UpdateInteractionText()
    {
        if (manualSystem == null) return;
        
        string newActionText = "";
        bool shouldShowIcon = true;
        
        if (isBurned)
        {
            shouldShowIcon = false;
        }
        else if (!hasTotem)
        {
            newActionText = placeTotemText;
        }
        else if (!hasOil)
        {
            newActionText = pourOilText;
        }
        else
        {
            newActionText = burnTotemText;
        }
        
        // Atualizar apenas se o texto mudou
        if (manualSystem.actionText != newActionText)
        {
            manualSystem.actionText = newActionText;
        }
        
        // Controlar visibilidade do ícone
        if (!shouldShowIcon && manualSystem.enabled)
        {
            manualSystem.ForceShowIcon(false);
            manualSystem.enabled = false;
        }
        else if (shouldShowIcon && !manualSystem.enabled)
        {
            manualSystem.enabled = true;
        }
    }
    
    public bool TryPlaceTotem(TotemPickup totem)
    {
        if (hasTotem || totem == null) return false;
        
        if (totem.GetTotemID() != altarID)
        {
            Debug.Log($"{wrongTotemText} - {totem.GetTotemName()} não é compatível com {altarName}!");
            return false;
        }
        
        // Colocar totem no slot
        currentTotem = totem;
        hasTotem = true;
        
        totem.transform.SetParent(totemSlot);
        totem.transform.localPosition = Vector3.zero;
        totem.transform.localRotation = Quaternion.identity;
        
        // Desativar física do totem
        Rigidbody rb = totem.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        
        Collider col = totem.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        // Esconder ícone do totem
        ManualInteractionSystem totemManual = totem.GetComponent<ManualInteractionSystem>();
        if (totemManual != null)
        {
            totemManual.ForceShowIcon(false);
            totemManual.enabled = false;
        }
        
        UpdateInteractionText();
        Debug.Log($"Totem {totem.GetTotemName()} colocado em {altarName}");
        
        return true;
    }
    
    public void StartPouringOil(GameObject progressHolder, RectTransform movingFillRect, OilJar oilJar)
    {
        if (!hasTotem || hasOil || isPouring || oilJar == null) return;
        
        isPouring = true;
        currentActionProgress = 0f;
        currentOilJar = oilJar; // Salvar referência do jarro
        
        // Iniciar UI de progresso
        if (movingFillRect != null)
        {
            float fillHeight = movingFillRect.rect.height;
            float fillStartY = -fillHeight;
            movingFillRect.anchoredPosition = new Vector2(0, fillStartY);
        }
        
        if (progressHolder != null)
            progressHolder.SetActive(true);
        
        // Iniciar animação do jarro
        if (currentOilJar != null)
        {
            currentOilJar.StartPouringAnimation();
        }
        
        // Mostrar plane de óleo
        if (oilPlane != null)
        {
            oilPlane.gameObject.SetActive(true);
            Vector3 pos = oilPlane.localPosition;
            pos.y = oilPlaneStartY;
            oilPlane.localPosition = pos;
        }
        
        Debug.Log($"Iniciando derramamento de óleo em {altarName}");
    }
    
    public void UpdatePouringOil(GameObject progressHolder, RectTransform movingFillRect, OilJar oilJar)
    {
        if (!isPouring || oilJar == null) return;
        
        currentActionProgress += Time.deltaTime;
        float progress = Mathf.Clamp01(currentActionProgress / pourOilTime);
        
        // Atualizar UI de progresso
        if (movingFillRect != null)
        {
            float fillHeight = movingFillRect.rect.height;
            float fillStartY = -fillHeight;
            float newY = Mathf.Lerp(fillStartY, 0, progress);
            movingFillRect.anchoredPosition = new Vector2(0, newY);
        }
        
        // Atualizar animação do jarro baseado no progresso
        if (currentOilJar != null)
        {
            currentOilJar.UpdatePouringAnimation(progress);
        }
        
        // Atualizar altura do plane de óleo
        if (oilPlane != null)
        {
            Vector3 pos = oilPlane.localPosition;
            pos.y = Mathf.Lerp(oilPlaneStartY, oilPlaneEndY, progress);
            oilPlane.localPosition = pos;
        }
        
        // Completar derramamento
        if (progress >= 1f)
        {
            CompletePouringOil(progressHolder, oilJar);
        }
    }
    
    void CompletePouringOil(GameObject progressHolder, OilJar oilJar)
    {
        isPouring = false;
        hasOil = true;
        
        // Parar animação do jarro
        if (currentOilJar != null)
        {
            currentOilJar.StopPouringAnimation();
        }
        
        // Destruir o jarro de óleo
        if (oilJar != null)
        {
            oilJar.Use();
            Destroy(oilJar.gameObject);
        }
        
        // Limpar referência do jarro
        currentOilJar = null;
        
        // Esconder UI de progresso
        if (progressHolder != null)
            progressHolder.SetActive(false);
        
        // Garantir que o plane está na posição final
        if (oilPlane != null)
        {
            Vector3 pos = oilPlane.localPosition;
            pos.y = oilPlaneEndY;
            oilPlane.localPosition = pos;
        }
        
        UpdateInteractionText();
        Debug.Log($"Óleo derramado com sucesso em {altarName}");
    }
    
    public void CancelPouringOil(GameObject progressHolder)
    {
        if (!isPouring) return;
        
        isPouring = false;
        currentActionProgress = 0f;
        
        // Parar animação do jarro quando cancelar
        if (currentOilJar != null)
        {
            currentOilJar.StopPouringAnimation();
            currentOilJar = null;
        }
        
        // Esconder UI de progresso
        if (progressHolder != null)
            progressHolder.SetActive(false);
        
        // Resetar plane de óleo
        if (oilPlane != null)
        {
            Vector3 pos = oilPlane.localPosition;
            pos.y = oilPlaneStartY;
            oilPlane.localPosition = pos;
            oilPlane.gameObject.SetActive(false);
        }
        
        Debug.Log($"Derramamento de óleo cancelado em {altarName}");
    }
    
    public void StartBurning(GameObject progressHolder, RectTransform movingFillRect)
    {
        if (!hasTotem || !hasOil || isBurning || isBurned) return;
        
        isBurning = true;
        currentActionProgress = 0f;
        
        // Iniciar UI de progresso
        if (movingFillRect != null)
        {
            float fillHeight = movingFillRect.rect.height;
            float fillStartY = -fillHeight;
            movingFillRect.anchoredPosition = new Vector2(0, fillStartY);
        }
        
        if (progressHolder != null)
            progressHolder.SetActive(true);
        
        Debug.Log($"Iniciando queima do totem em {altarName}");
    }
    
    public void UpdateBurning(GameObject progressHolder, RectTransform movingFillRect)
    {
        if (!isBurning) return;
        
        currentActionProgress += Time.deltaTime;
        float progress = Mathf.Clamp01(currentActionProgress / burnTime);
        
        // Atualizar UI de progresso
        if (movingFillRect != null)
        {
            float fillHeight = movingFillRect.rect.height;
            float fillStartY = -fillHeight;
            float newY = Mathf.Lerp(fillStartY, 0, progress);
            movingFillRect.anchoredPosition = new Vector2(0, newY);
        }
        
        // NÃO fazer o óleo descer aqui - só desce após o fogo acender
        
        // Completar queima
        if (progress >= 1f)
        {
            CompleteBurning(progressHolder);
        }
    }
    
    void CompleteBurning(GameObject progressHolder)
    {
        isBurning = false;
        isBurned = true;
        
        // Esconder UI de progresso
        if (progressHolder != null)
            progressHolder.SetActive(false);
        
        // Iniciar partículas de fogo
        if (fireParticles != null)
            fireParticles.Play();
        
        // **NOVA ADIÇÃO: Ativar fumaça junto com o fogo**
        if (smokeParticles != null)
        {
            smokeParticles.gameObject.SetActive(true);
            smokeParticles.Play();
            Debug.Log($"TotemAltar {altarName}: Fumaça ativada junto com o fogo.");
        }
        
        // Remover olhos em vez de destruir o totem
        RemoveTotemEyes();
        
        // AGORA SIM: Iniciar o efeito de queima com descida do óleo
        StartCoroutine(BurnTotemEffect());
        
        UpdateInteractionText();
        Debug.Log($"Totem queimado com sucesso em {altarName}! Fogo acendeu e óleo começará a descer.");
    }
    
    public void CancelBurning(GameObject progressHolder)
    {
        if (!isBurning) return;
        
        isBurning = false;
        currentActionProgress = 0f;
        
        // Esconder UI de progresso
        if (progressHolder != null)
            progressHolder.SetActive(false);
        
        Debug.Log($"Queima cancelada em {altarName}");
    }
    
    IEnumerator BurnTotemEffect()
    {
        // **MODIFICAÇÃO: Sincronizar descida do óleo com duração total (fogo + fade)**
        float totalFireTime = fireDuration + fireFadeDuration;
        float elapsedTime = 0f;
        
        Debug.Log($"TotemAltar {altarName}: Óleo descerá sincronizado com fogo total (queima + fade) durante {totalFireTime}s");
        
        while (elapsedTime < fireDuration)
        {
            elapsedTime += Time.deltaTime;
            float totalProgress = elapsedTime / totalFireTime; // Progresso baseado no tempo TOTAL
            
            // Fazer o óleo descer sincronizado com o tempo total (fogo + fade)
            if (oilDescendsWhileBurning && oilPlane != null)
            {
                Vector3 pos = oilPlane.localPosition;
                // Óleo desce durante fireDuration + fireFadeDuration
                pos.y = Mathf.Lerp(oilPlaneEndY, oilPlaneStartY, totalProgress);
                oilPlane.localPosition = pos;
            }
            
            yield return null;
        }
        
        // Iniciar fade do fogo (continuando a descida do óleo durante o fade)
        yield return StartCoroutine(FadeFireEmissionWithOil(elapsedTime, totalFireTime));
        
        // Garantir que o óleo chegou na posição final (sincronizado com fim do fade)
        if (oilPlane != null)
        {
            Vector3 pos = oilPlane.localPosition;
            pos.y = oilPlaneStartY;
            oilPlane.localPosition = pos;
        }
        
        // Destruir o portão se foi definido
        if (gateToDestroy != null)
        {
            Debug.Log($"TotemAltar {altarName}: Destruindo portão '{gateToDestroy.name}' após queimar o totem.");
            Destroy(gateToDestroy);
            gateToDestroy = null; // Limpar referência
        }
        
        // Aguardar tempo da fumaça antes de fazer fade (fumaça já está ativa)
        yield return new WaitForSeconds(smokeDuration);
        
        // Fazer fade da fumaça
        yield return StartCoroutine(FadeSmokeEmission());
        
        Debug.Log($"TotemAltar {altarName}: Processo completo finalizado - fogo e fumaça desapareceram gradualmente.");
        
        // Opcional: Notificar o GameManager que este altar foi completado
        // GameManager.Instance.OnAltarCompleted(altarID);
    }
    
    /// <summary>
    /// Faz fade gradual do fogo enquanto continua descendo o óleo
    /// </summary>
    IEnumerator FadeFireEmissionWithOil(float startElapsedTime, float totalFireTime)
    {
        if (fireParticles == null) yield break;
        
        Debug.Log($"TotemAltar {altarName}: Iniciando fade do fogo durante {fireFadeDuration} segundos (continuando descida do óleo).");
        
        // Salvar configurações originais das partículas
        var main = fireParticles.main;
        var emission = fireParticles.emission;
        
        float originalStartLifetime = main.startLifetime.constant;
        float originalRateOverTime = emission.rateOverTime.constant;
        
        // Salvar posição original do fogo para movimento
        Vector3 originalFirePosition = fireParticles.transform.position;
        Vector3 targetFirePosition = originalFirePosition + Vector3.down * fireDescentDistance;
        
        // Coletar materiais com emission dos renderers filhos
        List<Material> fireMaterials = new List<Material>();
        List<Color> originalEmissions = new List<Color>();
        
        Renderer[] fireRenderers = fireParticles.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in fireRenderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    fireMaterials.Add(mat);
                    originalEmissions.Add(mat.GetColor("_EmissionColor"));
                }
            }
        }
        
        // Fade gradual
        float fadeElapsedTime = 0f;
        while (fadeElapsedTime < fireFadeDuration)
        {
            fadeElapsedTime += Time.deltaTime;
            float currentTotalTime = startElapsedTime + fadeElapsedTime;
            
            // Progresso do fade (apenas para o fogo)
            float fadeProgress = fadeElapsedTime / fireFadeDuration;
            float inverseProgress = 1f - fadeProgress;
            
            // Progresso total (para o óleo)
            float totalProgress = currentTotalTime / totalFireTime;
            
            // Reduzir rate de emissão das partículas
            emission.rateOverTime = originalRateOverTime * inverseProgress;
            
            // Reduzir lifetime das partículas
            main.startLifetime = originalStartLifetime * inverseProgress;
            
            // Mover o fogo para baixo gradualmente
            fireParticles.transform.position = Vector3.Lerp(originalFirePosition, targetFirePosition, fadeProgress);
            
            // Reduzir emission dos materiais
            for (int i = 0; i < fireMaterials.Count; i++)
            {
                if (fireMaterials[i] != null)
                {
                    Color currentEmission = Color.Lerp(originalEmissions[i], Color.black, fadeProgress);
                    fireMaterials[i].SetColor("_EmissionColor", currentEmission);
                }
            }
            
            // **CONTINUAÇÃO: Atualizar descida do óleo durante o fade**
            if (oilDescendsWhileBurning && oilPlane != null)
            {
                Vector3 pos = oilPlane.localPosition;
                pos.y = Mathf.Lerp(oilPlaneEndY, oilPlaneStartY, totalProgress);
                oilPlane.localPosition = pos;
            }
            
            yield return null;
        }
        
        // Parar completamente
        fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // Garantir que materiais chegaram a 0
        foreach (Material mat in fireMaterials)
        {
            if (mat != null)
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
        
        // Garantir que o fogo chegou na posição final
        fireParticles.transform.position = targetFirePosition;
        
        // Aguardar um pouco para garantir que todas as partículas desapareceram
        yield return new WaitForSeconds(0.5f);
        
        // Destruir o objeto do fogo
        Destroy(fireParticles.gameObject);
        
        Debug.Log($"TotemAltar {altarName}: Fogo destruído após fade completo (desceu {fireDescentDistance}m). Óleo sincronizado.");
    }
    
    /// <summary>
    /// Faz fade gradual do emission dos materiais da fumaça
    /// </summary>
    IEnumerator FadeSmokeEmission()
    {
        if (smokeParticles == null) yield break;
        
        Debug.Log($"TotemAltar {altarName}: Iniciando fade da fumaça durante {smokeFadeDuration} segundos.");
        
        // Salvar configurações originais das partículas
        var main = smokeParticles.main;
        var emission = smokeParticles.emission;
        
        float originalStartLifetime = main.startLifetime.constant;
        float originalRateOverTime = emission.rateOverTime.constant;
        
        // Coletar materiais com emission dos renderers filhos
        List<Material> smokeMaterials = new List<Material>();
        List<Color> originalEmissions = new List<Color>();
        
        Renderer[] smokeRenderers = smokeParticles.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in smokeRenderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    smokeMaterials.Add(mat);
                    originalEmissions.Add(mat.GetColor("_EmissionColor"));
                }
            }
        }
        
        // Fade gradual
        float elapsedTime = 0f;
        while (elapsedTime < smokeFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float fadeProgress = elapsedTime / smokeFadeDuration;
            float inverseProgress = 1f - fadeProgress;
            
            // Reduzir rate de emissão das partículas
            emission.rateOverTime = originalRateOverTime * inverseProgress;
            
            // Reduzir lifetime das partículas (faz elas desaparecerem mais rápido)
            main.startLifetime = originalStartLifetime * inverseProgress;
            
            // Reduzir emission dos materiais
            for (int i = 0; i < smokeMaterials.Count; i++)
            {
                if (smokeMaterials[i] != null)
                {
                    Color currentEmission = Color.Lerp(originalEmissions[i], Color.black, fadeProgress);
                    smokeMaterials[i].SetColor("_EmissionColor", currentEmission);
                }
            }
            
            yield return null;
        }
        
        // Parar completamente
        smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // Garantir que materiais chegaram a 0
        foreach (Material mat in smokeMaterials)
        {
            if (mat != null)
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
        
        // Aguardar um pouco para garantir que todas as partículas desapareceram
        yield return new WaitForSeconds(0.5f);
        
        // Destruir o objeto da fumaça
        Destroy(smokeParticles.gameObject);
        
        Debug.Log($"TotemAltar {altarName}: Fumaça destruída após fade completo.");
    }
    
    /// <summary>
    /// Remove os objetos dos olhos do totem
    /// </summary>
    void RemoveTotemEyes()
    {
        if (currentTotem == null)
        {
            Debug.LogWarning($"TotemAltar {altarName}: Tentativa de remover olhos mas currentTotem é null!");
            return;
        }
        
        int eyesRemoved = 0;
        
        // Remover olho esquerdo
        if (leftEyeObject != null)
        {
            Debug.Log($"TotemAltar {altarName}: Removendo olho esquerdo '{leftEyeObject.name}' do totem {currentTotem.GetTotemName()}");
            Destroy(leftEyeObject);
            leftEyeObject = null; // Limpar referência
            eyesRemoved++;
        }
        
        // Remover olho direito
        if (rightEyeObject != null)
        {
            Debug.Log($"TotemAltar {altarName}: Removendo olho direito '{rightEyeObject.name}' do totem {currentTotem.GetTotemName()}");
            Destroy(rightEyeObject);
            rightEyeObject = null; // Limpar referência
            eyesRemoved++;
        }
        
        if (eyesRemoved > 0)
        {
            Debug.Log($"TotemAltar {altarName}: {eyesRemoved} objeto(s) de olhos removidos do totem {currentTotem.GetTotemName()}");
        }
        else
        {
            Debug.LogWarning($"TotemAltar {altarName}: Nenhum objeto de olhos foi definido no Inspector. Arraste os GameObjects dos olhos para os campos 'leftEyeObject' e 'rightEyeObject'.");
        }
    }
    
    // Getters para verificação de estado
    public bool HasTotem() { return hasTotem; }
    public bool HasOil() { return hasOil; }
    public bool IsBurned() { return isBurned; }
    public bool IsPouring() { return isPouring; }
    public bool IsBurning() { return isBurning; }
    public string GetAltarID() { return altarID; }
    
    void OnDrawGizmosSelected()
    {
        if (totemSlot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(totemSlot.position, Vector3.one * 0.5f);
        }
        
        // Mostrar o collider principal de interação
        if (mainInteractionCollider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = mainInteractionCollider.transform.localToWorldMatrix;
            
            if (mainInteractionCollider is BoxCollider)
            {
                BoxCollider box = mainInteractionCollider as BoxCollider;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (mainInteractionCollider is SphereCollider)
            {
                SphereCollider sphere = mainInteractionCollider as SphereCollider;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            
            Gizmos.matrix = Matrix4x4.identity;
        }
        
        // Mostrar conexão com o portão que será destruído
        if (gateToDestroy != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, gateToDestroy.transform.position);
            
            // Desenhar um X no portão para indicar que será destruído
            Vector3 gatePos = gateToDestroy.transform.position;
            float size = 1f;
            Gizmos.DrawLine(gatePos + Vector3.left * size + Vector3.up * size, 
                          gatePos + Vector3.right * size + Vector3.down * size);
            Gizmos.DrawLine(gatePos + Vector3.right * size + Vector3.up * size, 
                          gatePos + Vector3.left * size + Vector3.down * size);
        }
    }
    
    /// <summary>
    /// Método para configurar o altar automaticamente no editor
    /// </summary>
    [ContextMenu("Configurar Altar Automaticamente")]
    public void SetupAltarAutomatically()
    {
        // Encontrar ou criar collider principal
        if (mainInteractionCollider == null)
        {
            mainInteractionCollider = GetComponent<Collider>();
            
            if (mainInteractionCollider == null)
            {
                // Criar um BoxCollider padrão
                BoxCollider newCollider = gameObject.AddComponent<BoxCollider>();
                newCollider.size = new Vector3(2f, 2f, 2f);
                newCollider.center = Vector3.up;
                mainInteractionCollider = newCollider;
                Debug.Log($"TotemAltar {altarName}: BoxCollider principal criado automaticamente.");
            }
        }
        
        // Desabilitar colliders filhos que podem interferir
        if (totemSlot != null)
        {
            Collider[] childColliders = totemSlot.GetComponentsInChildren<Collider>();
            foreach (Collider childCol in childColliders)
            {
                if (childCol != mainInteractionCollider)
                {
                    childCol.enabled = false;
                }
            }
        }
        
        Debug.Log($"TotemAltar {altarName}: Configuração automática concluída!");
    }
    
    /// <summary>
    /// Encontra e lista todos os objetos filhos do totem atual (para ajudar na configuração)
    /// </summary>
    [ContextMenu("Listar Objetos do Totem")]
    public void FindEyeObjects()
    {
        if (currentTotem == null)
        {
            Debug.LogWarning($"TotemAltar {altarName}: Nenhum totem colocado no altar para analisar.");
            return;
        }
        
        Debug.Log($"=== Objetos filhos do totem {currentTotem.GetTotemName()} ===");
        
        Transform[] allChildren = currentTotem.GetComponentsInChildren<Transform>();
        
        foreach (Transform child in allChildren)
        {
            if (child == currentTotem.transform) continue;
            
            Debug.Log($"  • {child.name} (Path: {GetPath(child)})");
        }
        
        Debug.Log("=== Use os nomes acima para arrastar os objetos dos olhos no Inspector ===");
    }
    
    /// <summary>
    /// Valida se o altar está configurado corretamente
    /// </summary>
    [ContextMenu("Validar Configuração do Altar")]
    public void ValidateAltarConfiguration()
    {
        Debug.Log($"=== Validando configuração do {altarName} ===");
        
        // Verificar configurações básicas
        if (string.IsNullOrEmpty(altarID))
            Debug.LogWarning($"TotemAltar {altarName}: altarID está vazio!");
        
        if (totemSlot == null)
            Debug.LogWarning($"TotemAltar {altarName}: totemSlot não foi definido!");
        
        if (mainInteractionCollider == null)
            Debug.LogWarning($"TotemAltar {altarName}: mainInteractionCollider não foi definido!");
        
        // Verificar configurações de olhos
        if (leftEyeObject == null && rightEyeObject == null)
            Debug.LogWarning($"TotemAltar {altarName}: Nenhum objeto de olho foi definido!");
        
        if (leftEyeObject == null)
            Debug.LogWarning($"TotemAltar {altarName}: leftEyeObject não foi definido!");
        
        if (rightEyeObject == null)
            Debug.LogWarning($"TotemAltar {altarName}: rightEyeObject não foi definido!");
        
        // Verificar portão
        if (gateToDestroy == null)
            Debug.LogWarning($"TotemAltar {altarName}: gateToDestroy não foi definido! Nenhum portão será liberado.");
        else
            Debug.Log($"TotemAltar {altarName}: Portão '{gateToDestroy.name}' será destruído após queimar o totem.");
        
        // Verificar partículas
        if (fireParticles == null)
            Debug.LogWarning($"TotemAltar {altarName}: fireParticles não foi definido!");
        
        if (smokeParticles == null)
            Debug.LogWarning($"TotemAltar {altarName}: smokeParticles não foi definido!");
        
        // Verificar plane de óleo
        if (oilPlane == null)
            Debug.LogWarning($"TotemAltar {altarName}: oilPlane não foi definido!");
        
        Debug.Log($"=== Validação concluída para {altarName} ===");
    }
    
    /// <summary>
    /// Obtém o caminho completo de um Transform
    /// </summary>
    string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
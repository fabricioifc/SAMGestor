const FIRST_M  = ['João','Pedro','Carlos','Lucas','Mateus','Rafael','Thiago','Bruno','André','Felipe','Paulo','Marco'];
const FIRST_F  = ['Maria','Ana','Juliana','Fernanda','Camila','Beatriz','Larissa','Priscila','Vanessa','Patricia','Sandra','Marta'];
const SURNAMES = ['Silva','Santos','Souza','Oliveira','Lima','Costa','Pereira','Ferreira','Rodrigues','Alves','Nascimento','Araújo'];
const CITIES   = ['São Paulo','Curitiba','Florianópolis','Porto Alegre','Belo Horizonte','Caçador','Goiânia','Campinas','Recife','Salvador'];
const UF_LIST  = ['SP','PR','SC','RS','MG','GO','RJ','DF','PE','BA'];

const MARITAL   = ['Single','Married','Divorced','Widowed','StableUnion'];
const SHIRT     = ['PP','P','M','G','GG','XG'];
const PARENT_ST = ['Alive','Deceased','Unknown'];
const ALCOHOL   = ['None','Social','Moderate','Heavy'];
const EDU_LVL   = ['Incomplete','Complete','HighSchool','Undergraduate','Graduate','Postgraduate'];

function pick(arr, idx) { return arr[Math.abs(idx) % arr.length]; }
function pad(n, len)    { return String(n).padStart(len, '0'); }

function birthDate(vu, iter) {
    const year  = new Date().getFullYear() - (18 + ((vu * 7 + iter * 3) % 42));
    const month = pad(((vu + iter) % 12) + 1, 2);
    const day   = pad((iter % 28) + 1, 2);
    return `${year}-${month}-${day}`;
}

function cpfFazer(vu, iter)  { return `1${pad(vu, 5)}${pad(iter, 5)}`; }
function cpfServir(vu, iter) { return `2${pad(vu, 5)}${pad(iter, 5)}`; }

// ─── POST /api/registrations ──────────────────────────────────────────────────
export function createRegistrationPayload(vu, iter, retreatId) {
    const isMale = (vu + iter) % 2 === 0;
    const gender = isMale ? 'Male' : 'Female';
    const first  = pick(isMale ? FIRST_M : FIRST_F, vu + iter);
    const last   = pick(SURNAMES, vu * 3 + iter);

    return {
        retreatId,
        name:  { value: `${first} ${last}` },
        cpf:   { value: cpfFazer(vu, iter) },
        email: { value: `load.fazer.${vu}.${iter}@samtest.local` },

        phone:           `4${pad(19000000 + vu * 100 + iter, 8)}`,
        birthDate:       birthDate(vu, iter),
        gender,
        city:            pick(CITIES, vu),

        maritalStatus:   pick(MARITAL, vu + iter),
        pregnancy:       (gender === 'Female' && iter % 15 === 0) ? 'Weeks0To12' : 'NotApplicable',
        shirtSize:       pick(SHIRT, iter),
        weightKg:        55 + (vu % 50),
        heightCm:        160 + (iter % 35),
        profession:      'Testador de carga',
        streetAndNumber: `Rua das Flores, ${100 + vu}`,
        neighborhood:    'Centro',
        state:           pick(UF_LIST, vu),

        whatsapp:         null,
        facebookUsername: null,
        instagramHandle:  null,
        neighborPhone:   `4${pad(29000000 + vu, 8)}`,
        relativePhone:   `4${pad(39000000 + iter, 8)}`,

        fatherStatus: pick(PARENT_ST, vu),
        fatherName:   `${pick(FIRST_M, vu)} ${pick(SURNAMES, iter)}`,
        fatherPhone:  null,
        motherStatus: pick(PARENT_ST, iter),
        motherName:   `${pick(FIRST_F, vu)} ${pick(SURNAMES, vu)}`,
        motherPhone:  null,

        hadFamilyLossLast6Months:     false,
        familyLossDetails:            null,
        hasRelativeOrFriendSubmitted: false,
        submitterRelationship:        0,
        submitterNames:               null,

        religion:                     'Católico',
        previousUncalledApplications: 0,
        rahaminVidaCompleted:         0,

        alcoholUse:                pick(ALCOHOL, iter),
        smoker:                    false,
        usesDrugs:                 false,
        drugUseFrequency:          null,
        hasAllergies:              false,
        allergiesDetails:          null,
        hasMedicalRestriction:     false,
        medicalRestrictionDetails: null,
        takesMedication:           false,
        medicationsDetails:        null,
        physicalLimitationDetails:       null,
        recentSurgeryOrProcedureDetails: null,

        termsAccepted:  true,
        termsVersion:   'v1.0',
        marketingOptIn: true,
        clientIp:       null,
        userAgent:      null,
    };
}

// ─── POST /api/service-registrations
export function createServiceRegistrationPayload(vu, iter, retreatId, preferredSpaceId = null) {
    const isMale = (vu + iter) % 2 === 1;
    const gender = isMale ? 'Male' : 'Female';
    const first  = pick(isMale ? FIRST_M : FIRST_F, vu + iter + 7);
    const last   = pick(SURNAMES, vu * 5 + iter);

    return {
        retreatId,
        name:  { value: `${first} ${last}` },
        cpf:   { value: cpfServir(vu, iter) },
        email: { value: `load.servir.${vu}.${iter}@samtest.local` },

        phone:           `4${pad(49000000 + vu * 100 + iter, 8)}`,
        birthDate:       birthDate(vu + 3, iter + 2),
        gender,
        city:            pick(CITIES, iter),

        maritalStatus:   pick(MARITAL, iter),
        pregnancy:       'NotApplicable',
        shirtSize:       pick(SHIRT, vu),
        weightKg:        55 + (iter % 50),
        heightCm:        160 + (vu % 35),
        profession:      'Testador de carga',
        educationLevel:  pick(EDU_LVL, vu + iter),

        streetAndNumber: `Av. Brasil, ${200 + iter}`,
        neighborhood:    'Jardim',
        state:           pick(UF_LIST, iter),
        postalCode:      '89500000',
        whatsapp:        `4${pad(59000000 + vu, 8)}`,

        rahaminVidaCompleted:         0,
        previousUncalledApplications: 0,
        postRetreatLifeSummary:       'Minha vida foi transformada pelo retiro.',

        churchLifeDescription:         'Participo ativamente da comunidade paroquial.',
        prayerLifeDescription:         'Rezo o terço diariamente.',
        familyRelationshipDescription: 'Boa relação com a família.',
        selfRelationshipDescription:   'Busco crescimento contínuo na fé.',

        preferredSpaceId,

        termsAccepted:  true,
        termsVersion:   'v1.0',
        marketingOptIn: true,
        clientIp:       null,
        userAgent:      null,
    };
}
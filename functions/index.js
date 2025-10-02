const functions = require("firebase-functions");
const admin = require("firebase-admin");

admin.initializeApp();

const db = admin.firestore();

/**
 * A generic function to log document changes to the persistentLogs collection.
 * @param {functions.Change} change - The change object from the Firestore trigger.
 * @param {functions.EventContext} context - The event context.
 * @return {Promise<void>} A promise that resolves when the log is written.
 */
const logActivity = async (change, context) => {
  const { documentId } = context.params;
  const collectionName = context.resource.name.split("/").slice(-2, -1)[0];

  let actionType = "";
  const details = {};

  if (!change.before.exists && change.after.exists) {
    actionType = "Create";
    details.newValues = change.after.data();
  } else if (change.before.exists && change.after.exists) {
    actionType = "Update";
    details.oldValues = change.before.data();
    details.newValues = change.after.data();
  } else if (change.before.exists && !change.after.exists) {
    actionType = "Delete";
    details.oldValues = change.before.data();
  }

  // For now, PerformedBy is "System" as we can't easily get the user from a backend trigger.
  // A more advanced implementation might pass the user's ID in the write request from the client.
  const logEntry = {
    Timestamp: admin.firestore.FieldValue.serverTimestamp(),
    EntityType: collectionName,
    EntityId: documentId,
    ActionType: actionType,
    PerformedBy: "System (Cloud Function)",
    Details: details,
  };

  try {
    await db.collection("persistentLogs").add(logEntry);
    console.log(`Logged ${actionType} on ${collectionName}/${documentId}`);
  } catch (error) {
    console.error(`Failed to log activity for ${collectionName}/${documentId}`, error);
  }
};

// Create a trigger for each collection that needs auditing.
const collectionsToAudit = [
    "colaboradores",
    "computadores",
    "monitores",
    "perifericos",
    "manutencoes",
    "chamados",
    "redes"
];

collectionsToAudit.forEach(collection => {
  exports[`audit${collection}`] = functions.firestore
    .document(`${collection}/{documentId}`)
    .onWrite(logActivity);
});